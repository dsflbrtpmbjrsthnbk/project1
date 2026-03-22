{
    'name': 'InventoryHub Connector',
    'version': '16.0.1.0.0',
    'category': 'Productivity',
    'summary': 'Import and view aggregated inventory data from InventoryHub',
    'description': """
InventoryHub Connector
======================
This module allows you to import inventory data (titles, fields, and aggregated statistics)
from your InventoryHub application using a per-inventory API token.

Features:
- Store imported inventories with metadata (title, description, category, item count)
- View aggregated field statistics: min/max/avg for numbers, top values for text, true/false counts for checkboxes
- One-click import action that fetches live data from InventoryHub
- Read-only viewer: the module is designed for safe, display-only use
    """,
    'author': 'InventoryHub Team',
    'depends': ['base'],
    'data': [
        'security/ir.model.access.csv',
        'views/inventory_import_views.xml',
        'views/menu.xml',
    ],
    'installable': True,
    'application': True,
    'license': 'LGPL-3',
}
