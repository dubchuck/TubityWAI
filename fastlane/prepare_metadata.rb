require 'json'
require 'fileutils'

# Locate directories
FASTLANE_DIR = File.expand_path(File.dirname(__FILE__))
CONFIG_FILE = File.join(FASTLANE_DIR, 'app_store_config.json')
METADATA_DIR = File.join(FASTLANE_DIR, 'metadata')
SCREENSHOTS_DIR = File.join(FASTLANE_DIR, 'screenshots')

puts "=================================================="
puts "Fastlane Pre-Deployment Metadata Generator"
puts "=================================================="

unless File.exist?(CONFIG_FILE)
  puts "ERROR: Configuration file not found at #{CONFIG_FILE}"
  exit 1
end

config = JSON.parse(File.read(CONFIG_FILE))

# Helper to find ImageMagick command
def find_convert_cmd
  if system("which magick > /dev/null 2>&1")
    "magick"
  elsif system("which convert > /dev/null 2>&1")
    "convert"
  elsif File.exist?("/opt/homebrew/bin/magick")
    "/opt/homebrew/bin/magick"
  elsif File.exist?("/opt/homebrew/bin/convert")
    "/opt/homebrew/bin/convert"
  elsif File.exist?("/usr/local/bin/magick")
    "/usr/local/bin/magick"
  elsif File.exist?("/usr/local/bin/convert")
    "/usr/local/bin/convert"
  else
    "convert" # fallback
  end
end

CONVERT_CMD = find_convert_cmd
puts "Using convert command: #{CONVERT_CMD}"

# 1. Clear out old metadata and screenshots directories to prevent stale assets
puts "Cleaning old metadata at #{METADATA_DIR}..."
FileUtils.rm_rf(METADATA_DIR)
FileUtils.mkdir_p(METADATA_DIR)

puts "Cleaning old screenshots at #{SCREENSHOTS_DIR}..."
FileUtils.rm_rf(SCREENSHOTS_DIR)
FileUtils.mkdir_p(SCREENSHOTS_DIR)

# 2. Process locales
locales = config['locales'] || {}
locales.each do |locale, fields|
  locale_dir = File.join(METADATA_DIR, locale)
  FileUtils.mkdir_p(locale_dir)
  
  puts "Writing metadata files for locale: #{locale}"
  fields.each do |key, value|
    file_path = File.join(locale_dir, "#{key}.txt")
    File.write(file_path, value)
    puts "  - Created #{key}.txt"
  end
end

# 3. Process screenshots
screenshots_config = config['screenshots'] || {}
source_dir = File.expand_path(screenshots_config['source_directory'] || '../AppStore/Screenshots/Source', FASTLANE_DIR)
targets = screenshots_config['targets'] || []
files = screenshots_config['files'] || []

puts "Processing screenshots from source directory: #{source_dir}..."

unless File.directory?(source_dir)
  puts "ERROR: Source directory for screenshots not found: #{source_dir}"
  exit 1
end

locales.keys.each do |locale|
  locale_screenshot_dir = File.join(SCREENSHOTS_DIR, locale)
  FileUtils.mkdir_p(locale_screenshot_dir)
  
  files.each_with_index do |file_info, index|
    source_file = File.join(source_dir, file_info['source'])
    unless File.exist?(source_file)
      puts "WARNING: Source file #{source_file} not found. Skipping."
      next
    end
    
    # Process for each target resolution
    targets.each do |target|
      device = target['device_type']
      width = target['width']
      height = target['height']
      
      # Determine name structure: Fastlane deliver expects structure like:
      # screenshots/<locale>/<device>_<index>.png
      # Example: screenshots/en-US/iPhone14ProMax_1.png
      target_filename = "#{device}_#{index + 1}.png"
      target_path = File.join(locale_screenshot_dir, target_filename)
      
      puts "  - Generating #{target_filename} (#{width}x#{height}) for #{locale}"
      
      # Use convert to resize and force aspect ratio
      cmd = "#{CONVERT_CMD} '#{source_file}' -resize #{width}x#{height}! '#{target_path}'"
      unless system(cmd)
        puts "ERROR: Failed to resize screenshot #{source_file} for #{device}"
      end
    end
  end
end

puts "=================================================="
puts "Metadata and Screenshot Generation Completed!"
puts "=================================================="
