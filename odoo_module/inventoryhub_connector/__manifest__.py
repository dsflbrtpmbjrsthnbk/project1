{
    'name': 'InventoryHub Connector',
    'version': '1.0',
    'category': 'Inventory',
    'summary': 'Connector for InventoryHub API',
    'description': """
        Module for importing inventory data from InventoryHub API.
    """,
    'depends': ['base', 'stock'],
    'data': [
        'security/ir.model.access.csv',
        'views/inventory_import_views.xml',
    ],
    'installable': True,
    'application': True,
    'auto_install': False,
    'license': 'LGPL-3',
}
