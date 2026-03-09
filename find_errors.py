import sys

try:
    with open('build_detailed.log', 'rb') as f:
        content = f.read().decode('utf-8', errors='ignore')
    
    found = False
    with open('errors.txt', 'w', encoding='utf-8') as out:
        for line in content.splitlines():
            if 'error CS' in line:
                out.write(line + '\n')
                found = True
    
    if not found:
        print("No 'error CS' found in log.")
except Exception as e:
    print(e)
