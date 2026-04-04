from odoo import models, fields  # type: ignore

class InventoryHubField(models.Model):
    _name = 'inventoryhub.field'
    _description = 'InventoryHub Field Statistics'
    _order = 'name'

    inventory_id = fields.Many2one('inventoryhub.inventory', string='Inventory', required=True, ondelete='cascade', readonly=True)
    name = fields.Char(string='Field Title', required=True, readonly=True)
    external_field_id = fields.Char(string='External Field ID', readonly=True)
    field_type = fields.Selection(
        selection=[
            ('string', 'Text'),
            ('numeric', 'Numeric'),
            ('boolean', 'Boolean'),
            ('multiline', 'Multiline Text'),
            ('link', 'Link / URL'),
        ],
        string='Type',
        readonly=True,
    )
    stats_json = fields.Text(string='Raw Stats (JSON)', readonly=True)

    stat_min = fields.Float(string='Min', readonly=True, digits=(16, 4))
    stat_max = fields.Float(string='Max', readonly=True, digits=(16, 4))
    stat_avg = fields.Float(string='Average', readonly=True, digits=(16, 4))
    stat_top_values = fields.Text(string='Top Values', readonly=True)
    stat_non_null_count = fields.Integer(string='Non-Empty Count', readonly=True)
    stat_true_count = fields.Integer(string='True Count', readonly=True)
    stat_false_count = fields.Integer(string='False Count', readonly=True)
    stat_total_items = fields.Integer(string='Total Items', readonly=True)

    stat_summary = fields.Char(string='Statistics Summary', compute='_compute_stat_summary', store=False)

    def _compute_stat_summary(self):
        for rec in self:
            t = rec.field_type
            if t == 'numeric':
                if rec.stat_min or rec.stat_max or rec.stat_avg:
                    rec.stat_summary = f"Min: {rec.stat_min:.2f} | Max: {rec.stat_max:.2f} | Avg: {rec.stat_avg:.2f}"
                else:
                    rec.stat_summary = 'No data'
            elif t == 'boolean':
                total = (rec.stat_true_count or 0) + (rec.stat_false_count or 0)
                if total:
                    pct = round((rec.stat_true_count or 0) / total * 100)
                    rec.stat_summary = f"True: {rec.stat_true_count} ({pct}%) | False: {rec.stat_false_count}"
                else:
                    rec.stat_summary = 'No data'
            elif t == 'link':
                rec.stat_summary = f"Non-null: {rec.stat_non_null_count or 0} / {rec.stat_total_items or 0}"
            elif t in ('string', 'multiline'):
                if rec.stat_top_values:
                    rec.stat_summary = f"Top values: {rec.stat_top_values[:80]}"
                else:
                    rec.stat_summary = 'No data'
            else:
                rec.stat_summary = ''
