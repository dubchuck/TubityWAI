import sys
import os
from PIL import Image

def process_glass_logo(input_path, output_path, bg_threshold=8, min_glass_alpha=45, alpha_multiplier=1.2):
    """
    Converts a black-background glassmorphic neon logo to RGBA, making the background transparent,
    preserving bright neon outlines, and converting the glass interior into a clean semi-transparent frosted layer.
    Trims the transparent borders and adds padding.
    """
    if not os.path.exists(input_path):
        print(f"Error: Input file {input_path} does not exist.")
        return False

    print(f"Loading image from {input_path}...")
    img = Image.open(input_path).convert("RGBA")
    datas = img.getdata()

    print("Processing pixels for glassmorphism and transparency...")
    newData = []
    for item in datas:
        r, g, b, a = item
        # Use maximum of R, G, B as brightness indicator
        brightness = max(r, g, b)
        
        if brightness < bg_threshold:
            # Clear background
            newData.append((0, 0, 0, 0))
        else:
            # Check if it's a bright neon outline or a glossy highlight
            if brightness > 100:
                alpha = min(255, int(brightness * alpha_multiplier))
                newData.append((r, g, b, alpha))
            else:
                # Glass fill - enforce a nice semi-transparent frosted glass alpha range
                alpha = min(255, min_glass_alpha + int(brightness * 0.4))
                # Add a subtle frosted tint to make the glass stand out
                newData.append((r, g, b, alpha))

    img.putdata(newData)

    print("Trimming transparent borders...")
    bbox = img.getbbox()
    if bbox:
        trimmed_img = img.crop(bbox)
        
        # Add 4px padding for Unity sprite compatibility
        padding = 4
        padded_width = trimmed_img.width + padding * 2
        padded_height = trimmed_img.height + padding * 2
        
        final_img = Image.new("RGBA", (padded_width, padded_height), (0, 0, 0, 0))
        final_img.paste(trimmed_img, (padding, padding))
        
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        final_img.save(output_path, "PNG")
        print(f"Success! Glassmorphic title saved to {output_path}")
        return True
    else:
        print("Error: Trim failed, image is completely transparent.")
        return False

if __name__ == "__main__":
    if len(sys.argv) < 3:
        # Default run for the current session asset
        input_file = "/Users/jeremy/.gemini/antigravity-ide/brain/602aad2e-dfd6-44f0-a38d-4e350c2be9cf/tubityx_glass_1785968823368.png"
        output_file = "/Users/jeremy/Documents/dubchuck/TubityWAI/Assets/Resources/tubityx_title.png"
    else:
        input_file = sys.argv[1]
        output_file = sys.argv[2]
        
    process_glass_logo(input_file, output_file)
