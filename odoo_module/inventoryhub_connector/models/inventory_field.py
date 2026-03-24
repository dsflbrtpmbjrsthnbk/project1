from odoo import fields, models

class InventoryField(models.Model):
   
    _name = 'inventoryhub.field'
    _description = 'InventoryHub – Imported Field Statistics'
    _order = 'id'

    inventory_id = fields.Many2one(
        comodel_name='inventoryhub.inventory',
        string='Inventory',
        required=True,
        ondelete='cascade',
    )
    name = fields.Char(
        string='Field Title',
        required=True,
        readonly=True,
    )
    external_field_id = fields.Char(
        string='External Field ID',
        readonly=True,
    )
    field_type = fields.Selection(
        selection=[
            ('String', 'Text'),
            ('Multiline', 'Multi-line Text'),
            ('Numeric', 'Number'),
            ('Link', 'URL / Link'),
            ('Boolean', 'Checkbox'),
        ],
        string='Type',
        readonly=True,
    )

    stat_count = fields.Integer(
        string='Non-Empty Count',
        readonly=True,
        help='Number of items that have a value for this field.',
    )
    stats_json = fields.Text(
        string='Raw JSON Stats',
        readonly=True,
    )

    stat_min = fields.Float(
        string='Min',
        readonly=True,
    )
    stat_max = fields.Float(
        string='Max',
        readonly=True,
    )
    stat_avg = fields.Float(
        string='Avg',
        readonly=True,
    )

    stat_top_values = fields.Text(
        string='Top Values',
        readonly=True,
        help='Most frequent values with their occurrences (e.g., "Apple (10), Banana (5)").',
    )

    stat_true_count = fields.Integer(
        string='True Count',
        readonly=True,
    )
    stat_false_count = fields.Integer(
        string='False Count',
        readonly=True,
    )
