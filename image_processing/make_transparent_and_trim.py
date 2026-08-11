import sys
import os
from PIL import Image

def make_transparent_and_trim(input_path, output_path, brightness_threshold=15, alpha_multiplier=1.3):
    """
    Converts a black-background image to transparent RGBA based on pixel brightness,
    and crops (trims) the transparent borders to fit the content exactly.
    """
    if not os.path.exists(input_path):
        print(f"Error: Input file {input_path} does not exist.")
        return False

    print(f"Loading image from {input_path}...")
    img = Image.open(input_path).convert("RGBA")
    datas = img.getdata()

    print("Processing pixels for transparency...")
    newData = []
    for item in datas:
        r, g, b, a = item
        # Calculate brightness as the max of R, G, B
        brightness = max(r, g, b)
        if brightness < brightness_threshold:
            # Fully transparent for dark/black pixels
            newData.append((0, 0, 0, 0))
        else:
            # Set alpha based on brightness to preserve neon glows
            alpha = min(255, int(brightness * alpha_multiplier))
            newData.append((r, g, b, alpha))

    img.putdata(newData)

    print("Trimming transparent borders...")
    # getbbox() returns the bounding box of non-zero pixels (non-transparent pixels)
    bbox = img.getbbox()
    if bbox:
        trimmed_img = img.crop(bbox)
        
        # Add a small padding (e.g. 4px) to prevent pixel bleeding/clipping in Unity
        padding = 4
        padded_width = trimmed_img.width + padding * 2
        padded_height = trimmed_img.height + padding * 2
        
        final_img = Image.new("RGBA", (padded_width, padded_height), (0, 0, 0, 0))
        final_img.paste(trimmed_img, (padding, padding))
        
        # Save output
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        final_img.save(output_path, "PNG")
        print(f"Success! Processed, trimmed, and padded image saved to {output_path}")
        return True
    else:
        print("Error: Image is completely transparent.")
        return False

if __name__ == "__main__":
    # Default paths for this task
    input_file = "/Users/jeremy/.gemini/antigravity-ide/brain/602aad2e-dfd6-44f0-a38d-4e350c2be9cf/tubityx_uniform_1785942419875.png"
    output_file = "/Users/jeremy/Documents/dubchuck/TubityWAI/Assets/Resources/tubityx_title.png"
    
    make_transparent_and_trim(input_file, output_file)
