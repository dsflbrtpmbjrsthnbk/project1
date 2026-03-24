import requests
import json
import logging
from odoo import api, fields, models, _
from odoo.exceptions import UserError

_logger = logging.getLogger(__name__)

class InventoryImport(models.Model):
    _name = 'inventoryhub.inventory'
    _description = 'InventoryHub – Imported Inventory'
    _order = 'write_date desc'

    name = fields.Char(string='Inventory Title', required=True, readonly=True, default='New Import')
    external_id = fields.Char(string='External ID (UUID)', copy=False)
    description = fields.Text(string='Description', readonly=True)
    category = fields.Char(string='Category', readonly=True)
    owner = fields.Char(string='Owner', readonly=True)
    
    api_base_url = fields.Char(string='InventoryHub Base URL', required=True, default='https://project1-vkwm.onrender.com')
    api_token = fields.Char(string='API Token', required=True)
    item_count = fields.Integer(string='Item Count', readonly=True, default=0)
    last_synced = fields.Datetime(string='Last Synced', readonly=True)
    is_public = fields.Boolean(string='Is Public', readonly=True)
    external_created_at = fields.Datetime(string='External Created At', readonly=True)

    field_ids = fields.One2many('inventoryhub.field', 'inventory_id', string='Fields Statistics', readonly=True)

    def action_import_from_inventoryhub(self):
        """Метод импорта данных из внешней системы .NET"""
        self.ensure_one()
        
        if not self.external_id or not self.api_token:
            raise UserError(_("Please fill in UUID and API Token first."))

        url = f"{self.api_base_url.rstrip('/')}/api/inventory/{self.external_id}"
        
        try:
            _logger.info("Importing from InventoryHub: %s", url)
            response = requests.get(url, headers={'X-Api-Token': self.api_token}, timeout=20)
            
            if response.status_code != 200:
                raise UserError(_("Server returned error %s: %s") % (response.status_code, response.text[:200]))
            
            data = response.json()
        except Exception as e:
            raise UserError(_("Connection failed: %s") % str(e))

        self.write({
            'name': data.get('title', self.name),
            'description': data.get('description', ''),
            'category': data.get('category', ''),
            'owner': data.get('owner', ''),
            'item_count': data.get('itemCount', 0),
            'is_public': data.get('isPublic', False),
            'external_created_at': data.get('createdAt'),
            'last_synced': fields.Datetime.now(),
        })

        self.field_ids.unlink()
        
        for f in data.get('fields', []):
            stats = f.get('stats', {})
            f_type = f.get('type', 'String')
            
            vals = {
                'inventory_id': self.id,
                'name': f.get('title', 'Unknown'),
                'field_type': f_type,
                'stat_count': stats.get('count') or stats.get('totalNonEmpty') or 0,
            }
            
            if f_type == 'Numeric':
                vals.update({
                    'stat_min': stats.get('min') or 0.0,
                    'stat_max': stats.get('max') or 0.0,
                    'stat_avg': stats.get('avg') or 0.0,
                })
            elif f_type in ('String', 'Multiline'):
                top = stats.get('topValues', [])
                if top:
                    vals['stat_top_values'] = ', '.join([f"{t.get('value')} ({t.get('count')})" for t in top])

            self.env['inventoryhub.field'].create(vals)

        return True
