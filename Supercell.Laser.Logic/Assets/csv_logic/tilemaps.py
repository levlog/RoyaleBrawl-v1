import csv
import json

# Input CSV and output JSON paths
csv_file = "maps.csv"
json_file = "tilemaps.json"

tilemaps = {}

with open(csv_file, newline="") as f:
    reader = csv.reader(f)
    current_map_name = None
    current_rows = []

    for row in reader:
        # Each row format: id, map_name (optional), row_data
        id_field = row[0].strip() if len(row) > 0 else ""
        name_field = row[1].strip() if len(row) > 1 else ""
        data_field = row[2].strip() if len(row) > 2 else ""

        if name_field:  # New map starts
            # Save previous map if exists
            if current_map_name and current_rows:
                height = len(current_rows)
                width = len(current_rows[0]) if current_rows else 0
                tilemaps[current_map_name] = {
                    "HEIGHT": height,
                    "WIDTH": width,
                    "Data": "".join(current_rows)
                }
            # Reset for new map
            current_map_name = name_field
            current_rows = [data_field]
        else:
            # Continuation of the current map
            if current_map_name:
                current_rows.append(data_field)

    # Save the last map
    if current_map_name and current_rows:
        height = len(current_rows)
        width = len(current_rows[0]) if current_rows else 0
        tilemaps[current_map_name] = {
            "HEIGHT": height,
            "WIDTH": width,
            "Data": "".join(current_rows)
        }

# Write JSON file
with open(json_file, "w") as f:
    json.dump(tilemaps, f, indent=4)

print(f"Converted {csv_file} → {json_file}")
