import requests
import json
import logging
from odoo import api, fields, models
from odoo.exceptions import UserError

_logger = logging.getLogger(__name__)


class InventoryImport(models.Model):
    """
    Represents an inventory imported from InventoryHub.
    Each record corresponds to one inventory identified by its UUID.
    """
    _name = 'inventoryhub.inventory'
    _description = 'InventoryHub – Imported Inventory'
    _order = 'write_date desc'
    _rec_name = 'name'

    # ── Core identity ─────────────────────────────────────────────────────────
    name = fields.Char(
        string='Inventory Title',
        required=True,
        readonly=True,
        default='New Import',
    )
    external_id = fields.Char(
        string='External ID (UUID)',
        help='The UUID of this inventory in InventoryHub. Used to construct the API URL.',
        copy=False,
    )
    description = fields.Text(
        string='Description',
        readonly=True,
    )
    category = fields.Char(
        string='Category',
        readonly=True,
    )
    owner = fields.Char(
        string='Owner',
        readonly=True,
    )
    is_public = fields.Boolean(
        string='Public',
        readonly=True,
    )

    # ── API connection settings ───────────────────────────────────────────────
    api_base_url = fields.Char(
        string='InventoryHub Base URL',
        required=True,
        default='https://project1-vkwm.onrender.com',
        help='Base URL of your InventoryHub instance, e.g., https://project1-vkwm.onrender.com',
    )
    api_token = fields.Char(
        string='API Token',
        required=True,
        help='The per-inventory API token generated from the inventory Settings tab in InventoryHub.',
    )

    # ── Aggregated counts ─────────────────────────────────────────────────────
    item_count = fields.Integer(
        string='Item Count',
        readonly=True,
        default=0,
    )

    # ── Timestamps ────────────────────────────────────────────────────────────
    external_created_at = fields.Datetime(
        string='Created in InventoryHub',
        readonly=True,
    )
    external_updated_at = fields.Datetime(
        string='Updated in InventoryHub',
        readonly=True,
    )
    last_synced = fields.Datetime(
        string='Last Synced',
        readonly=True,
    )

    # ── Related fields ────────────────────────────────────────────────────────
    field_ids = fields.One2many(
        comodel_name='inventoryhub.field',
        inverse_name='inventory_id',
        string='Fields & Statistics',
        readonly=True,
    )
    field_count = fields.Integer(
        string='# Fields',
        compute='_compute_field_count',
        store=True,
    )

    @api.depends('field_ids')
    def _compute_field_count(self):
        for rec in self:
            rec.field_count = len(rec.field_ids)

    # ── Computed API URL ──────────────────────────────────────────────────────
    api_url = fields.Char(
        string='Full API URL',
        compute='_compute_api_url',
    )

    @api.depends('api_base_url', 'external_id')
    def _compute_api_url(self):
        for rec in self:
            if rec.api_base_url and rec.external_id:
                base = rec.api_base_url.rstrip('/')
                rec.api_url = f'{base}/api/inventory/{rec.external_id}'
            else:
                rec.api_url = ''

    # ── Import action ─────────────────────────────────────────────────────────
    def action_import_from_inventoryhub(self):
        """
        Fetch aggregated data from the InventoryHub REST API and update this record.
        Called from the form view button or the list-view action.
        """
        self.ensure_one()

        if not self.api_token:
            raise UserError('Please set the API Token before importing.')
        if not self.external_id:
            raise UserError('Please set the External ID (UUID) before importing.')
        if not self.api_base_url:
            raise UserError('Please set the InventoryHub Base URL before importing.')

        url = self.api_url
        _logger.info('InventoryHub Connector: fetching %s', url)

        try:
            response = requests.get(
                url,
                headers={'X-Api-Token': self.api_token},
                timeout=30,
            )
        except requests.exceptions.RequestException as exc:
            raise UserError(f'Network error while contacting InventoryHub:\n{exc}') from exc

        if response.status_code == 401:
            raise UserError('InventoryHub returned 401 Unauthorized – check that the API Token is correct.')
        if response.status_code == 403:
            raise UserError('InventoryHub returned 403 Forbidden – the token does not match this inventory.')
        if response.status_code == 404:
            raise UserError('InventoryHub returned 404 – inventory not found. Check the External ID.')
        if not response.ok:
            raise UserError(f'InventoryHub returned HTTP {response.status_code}:\n{response.text[:500]}')

        try:
            data = response.json()
        except ValueError as exc:
            raise UserError(f'Could not parse JSON response from InventoryHub:\n{exc}') from exc

        # Parse ISO timestamps (Odoo expects naive UTC datetimes)
        def _parse_dt(val):
            if not val:
                return False
            try:
                from datetime import datetime, timezone
                dt = datetime.fromisoformat(val.rstrip('Z').replace('Z', '+00:00'))
                return dt.astimezone(timezone.utc).replace(tzinfo=None)
            except Exception:
                return False

        self.write({
            'name': data.get('title', self.name),
            'description': data.get('description') or '',
            'category': data.get('category') or '',
            'owner': data.get('owner') or '',
            'is_public': data.get('isPublic', False),
            'item_count': data.get('itemCount', 0),
            'external_created_at': _parse_dt(data.get('createdAt')),
            'external_updated_at': _parse_dt(data.get('updatedAt')),
            'last_synced': fields.Datetime.now(),
        })

        # Re-create field statistics (delete old, insert new)
        self.field_ids.unlink()

        FieldModel = self.env['inventoryhub.field']
        for field_data in data.get('fields', []):
            stats = field_data.get('stats', {})
            field_type = field_data.get('type', 'String')

            vals = {
                'inventory_id': self.id,
                'name': field_data.get('title', 'Unknown'),
                'external_field_id': field_data.get('id', ''),
                'field_type': field_type,
                'stats_json': json.dumps(stats, indent=2),
            }

            if field_type == 'Numeric':
                vals.update({
                    'stat_count': stats.get('count', 0),
                    'stat_min': stats.get('min') or 0.0,
                    'stat_max': stats.get('max') or 0.0,
                    'stat_avg': stats.get('avg') or 0.0,
                })
            elif field_type in ('String', 'Multiline'):
                top = stats.get('topValues', [])
                vals.update({
                    'stat_count': stats.get('totalNonEmpty', 0),
                    'stat_top_values': ', '.join(
                        f"{t['value']} ({t['count']})" for t in top
                    ),
                })
            elif field_type == 'Boolean':
                vals.update({
                    'stat_count': stats.get('count', 0),
                    'stat_true_count': stats.get('trueCount', 0),
                    'stat_false_count': stats.get('falseCount', 0),
                })
            elif field_type == 'Link':
                vals.update({
                    'stat_count': stats.get('nonNullCount', 0),
                })

            FieldModel.create(vals)

        return {
            'type': 'ir.actions.client',
            'tag': 'display_notification',
            'params': {
                'title': 'Import Successful',
                'message': f'Inventory "{self.name}" synced: {self.item_count} items, {len(data.get("fields", []))} fields.',
                'type': 'success',
                'sticky': False,
            },
        }
