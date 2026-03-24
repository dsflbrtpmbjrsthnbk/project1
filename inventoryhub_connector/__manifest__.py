{
    'name': 'InventoryHub Connector',
    'version': '16.0.1.0.0',
    'summary': 'Import and view aggregated inventory data from InventoryHub',
    'description': """
        InventoryHub Connector
        ======================
        This module allows you to connect to an external InventoryHub (.NET) application
        and import aggregated inventory statistics using a per-inventory API token.

        Features:
        - Store imported inventory metadata (title, fields, stats)
        - View list of imported inventories and their details
        - One-click import/refresh using the API token
        - Optional: create items in InventoryHub from Odoo
    """,
    'author': 'Course Project',
    'category': 'Productivity',
    'depends': ['base', 'web'],
    'data': [
        'security/ir.model.access.csv',
        'views/inventory_import_views.xml',
    ],
    'installable': True,
    'application': True,
    'license': 'LGPL-3',
}
