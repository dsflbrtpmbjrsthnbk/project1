from odoo import models, fields, api  # type: ignore
import requests
import json
import logging

_logger = logging.getLogger(__name__)

class InventoryHubInventory(models.Model):
    _name = 'inventoryhub.inventory'
    _description = 'InventoryHub Imported Inventory'
    _order = 'last_synced desc, name'

    name = fields.Char(string='Inventory Title', required=True, default='(Fetch from API)')
    description = fields.Text(string='Description', readonly=True)
    external_id = fields.Char(string='External ID (UUID)', required=True)
    api_token = fields.Char(string='API Token', required=True)
    api_url = fields.Char(string='InventoryHub Base URL', required=True, default='https://project1-vkwm.onrender.com')

    category = fields.Char(string='Category', readonly=True)
    owner = fields.Char(string='Owner', readonly=True)
    is_public = fields.Boolean(string='Public', readonly=True)
    item_count = fields.Integer(string='Item Count', readonly=True)
    last_synced = fields.Datetime(string='Last Synced', readonly=True)
    created_at = fields.Datetime(string='Created At', readonly=True)
    updated_at = fields.Datetime(string='Updated At', readonly=True)

    field_ids = fields.One2many('inventoryhub.field', 'inventory_id', string='Fields & Statistics', readonly=True)
    field_count = fields.Integer(string='Field Count', compute='_compute_field_count', store=False)

    @api.depends('field_ids')
    def _compute_field_count(self):
        for rec in self:
            rec.field_count = len(rec.field_ids)

    def action_import(self):
        self.ensure_one()
        ext_id = (self.external_id or "").strip()
        if "/" in ext_id:
            ext_id = ext_id.split("/")[-1]

        base_url = (self.api_url or "").strip().rstrip("/")
        url = f"{base_url}/api/inventory/{ext_id}"
        headers = {'X-Api-Token': (self.api_token or "").strip()}

        try:
            response = requests.get(url, headers=headers, timeout=60)
        except requests.exceptions.RequestException as e:
            raise models.ValidationError(f"Could not connect to InventoryHub: {e}")

        if response.status_code == 401:
            raise models.ValidationError('API token is missing.')
        if response.status_code == 403:
            raise models.ValidationError('Invalid API token.')
        if response.status_code == 404:
            raise models.ValidationError(f"Inventory ID '{self.external_id}' not found.")
        if not response.ok:
            raise models.ValidationError(f"InventoryHub returned HTTP {response.status_code}")

        try:
            data = response.json()
        except Exception:
            raise models.ValidationError("Failed to parse JSON response.")

        self.write({
            'name': data.get('title', self.name),
            'description': data.get('description'),
            'category': data.get('category'),
            'owner': data.get('owner'),
            'is_public': data.get('isPublic', False),
            'item_count': data.get('itemCount', 0),
            'last_synced': fields.Datetime.now(),
            'created_at': self._parse_dt(data.get('createdAt')),
            'updated_at': self._parse_dt(data.get('updatedAt')),
        })

        self.field_ids.unlink()
        Field = self.env['inventoryhub.field']
        for f in data.get('fields', []):
            stats = f.get('stats', {})
            top_raw = stats.get('topValues', [])
            if top_raw and isinstance(top_raw[0], dict):
                top_str = ', '.join(f"{tv['value']} ({tv['count']})" for tv in top_raw)
            else:
                top_str = ', '.join(str(v) for v in top_raw)

            Field.create({
                'inventory_id': self.id,
                'name': f.get('title', ''),
                'field_type': (f.get('type') or 'String').lower(),
                'external_field_id': f.get('id', ''),
                'stats_json': json.dumps(stats),
                'stat_min': stats.get('min'),
                'stat_max': stats.get('max'),
                'stat_avg': stats.get('avg'),
                'stat_top_values': top_str or None,
                'stat_true_count': stats.get('trueCount'),
                'stat_false_count': stats.get('falseCount'),
                'stat_non_null_count': stats.get('nonNullCount') or stats.get('totalNonEmpty'),
                'stat_total_items': stats.get('totalItems') or data.get('itemCount', 0),
            })

        return {
            'type': 'ir.actions.client',
            'tag': 'display_notification',
            'params': {
                'title': 'Import Successful',
                'message': f"Imported '{self.name}'",
                'type': 'success',
                'sticky': False,
            },
        }

    @staticmethod
    def _parse_dt(value):
        if not value:
            return False
        try:
            from datetime import datetime, timezone
            dt = datetime.fromisoformat(value.replace('Z', '+00:00'))
            return dt.astimezone(timezone.utc).replace(tzinfo=None)
        except Exception:
            return False
