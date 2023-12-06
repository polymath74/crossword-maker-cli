from fontTools.ttLib import TTFont

def extract_horizontal_metrics(font_path):
    # Load the TTF file
    font = TTFont(font_path)

    # Check if 'hmtx' table is present
    if 'hmtx' not in font:
        print("Error: 'hmtx' table not found in the font.")
        return

    # Get the metrics for each glyph
    hmtx_table = font['hmtx']
    for glyph_id, data in enumerate(hmtx_table.metrics):
        print(glyph_id, data)
        if data != '.notdef' and data != '.null':
            (advance_width, left_side_bearing) = data
            print(f"Glyph ID: {glyph_id}, Advance Width: {advance_width}, Left Side Bearing: {left_side_bearing}")

if __name__ == "__main__":
    font_path = r'fonts/FreeSans.ttf'
    extract_horizontal_metrics(font_path)
