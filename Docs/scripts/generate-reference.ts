/**
 * Generate reference documentation from source files
 * 
 * This script parses the server's ConfigService.cs to extract
 * configuration entries and generates MDX documentation.
 * 
 * Usage: npm run generate
 */

import { readFileSync, writeFileSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '../..');

// Source files
const CONFIG_SERVICE_PATH = resolve(ROOT, 'Server/Bloodcraftplus/Services/ConfigService.cs');
const OUTPUT_PATH = resolve(__dirname, '../src/content/reference/config-generated.mdx');

interface ConfigEntry {
  section: string;
  key: string;
  type: string;
  defaultValue: string;
  description: string;
}

/**
 * Parse ConfigService.cs to extract ConfigEntries
 */
function parseConfigService(content: string): ConfigEntry[] {
  const entries: ConfigEntry[] = [];

  // Match ConfigEntryDefinition constructor calls
  // Pattern: new ConfigEntryDefinition("Section", "Key", defaultValue, "Description")
  const entryRegex = /new\s+ConfigEntryDefinition\s*\(\s*"([^"]+)"\s*,\s*"([^"]+)"\s*,\s*([^,]+?)\s*,\s*"([^"]+)"\s*\)/gs;

  let match;
  while ((match = entryRegex.exec(content)) !== null) {
    const [, section, key, defaultRaw, description] = match;

    // Determine type from default value
    let type = 'string';
    let defaultValue = defaultRaw.trim();

    if (defaultValue === 'false' || defaultValue === 'true') {
      type = 'bool';
    } else if (defaultValue.endsWith('f')) {
      type = 'float';
      defaultValue = defaultValue.slice(0, -1);
    } else if (/^-?\d+$/.test(defaultValue)) {
      type = 'int';
    } else if (defaultValue.startsWith('"')) {
      type = 'string';
      defaultValue = defaultValue.slice(1, -1); // Remove quotes
    } else if (defaultValue.startsWith('$"') || defaultValue.includes('{')) {
      // Interpolated string - extract the content
      type = 'string';
      defaultValue = '(see code)';
    }

    entries.push({
      section,
      key,
      type,
      defaultValue,
      description: description.replace(/\\n/g, ' ').trim(),
    });
  }

  return entries;
}

/**
 * Group entries by section
 */
function groupBySection(entries: ConfigEntry[]): Map<string, ConfigEntry[]> {
  const grouped = new Map<string, ConfigEntry[]>();

  for (const entry of entries) {
    if (!grouped.has(entry.section)) {
      grouped.set(entry.section, []);
    }
    grouped.get(entry.section)!.push(entry);
  }

  return grouped;
}

/**
 * Generate MDX content
 */
function generateMDX(grouped: Map<string, ConfigEntry[]>): string {
  const lines: string[] = [];

  lines.push(`# Configuration Reference (Generated)`);
  lines.push('');
  lines.push(`> This file is auto-generated from \`Server/Bloodcraftplus/Services/ConfigService.cs\`.`);
  lines.push(`> Last generated: ${new Date().toISOString().split('T')[0]}`);
  lines.push('');
  lines.push(`<Callout type="info">`);
  lines.push(`  Configuration file location: \`BepInEx/config/io.zfolmt.Bloodcraft.cfg\``);
  lines.push(`</Callout>`);
  lines.push('');
  lines.push('---');
  lines.push('');

  // Section order from ConfigService.cs
  const sectionOrder = [
    'General',
    'StarterKit',
    'Quests',
    'Leveling',
    'Prestige',
    'Expertise',
    'Legacies',
    'Professions',
    'Familiars',
    'Classes',
  ];

  for (const section of sectionOrder) {
    const entries = grouped.get(section);
    if (!entries || entries.length === 0) continue;

    lines.push(`## ${section}`);
    lines.push('');
    lines.push('| Option | Type | Default | Description |');
    lines.push('|--------|------|---------|-------------|');

    for (const entry of entries) {
      const defaultDisplay = entry.type === 'string'
        ? `\`"${entry.defaultValue}"\``
        : `\`${entry.defaultValue}\``;

      // Escape pipes in description
      const desc = entry.description.replace(/\|/g, '\\|');

      lines.push(`| \`${entry.key}\` | ${entry.type} | ${defaultDisplay} | ${desc} |`);
    }

    lines.push('');
    lines.push('---');
    lines.push('');
  }

  return lines.join('\n');
}

/**
 * Main entry point
 */
function main() {
  console.log('📚 Generating reference documentation...');

  // Check if source file exists
  if (!existsSync(CONFIG_SERVICE_PATH)) {
    console.error(`❌ Config service not found: ${CONFIG_SERVICE_PATH}`);
    console.log('   This is expected when building without the full repo.');
    console.log('   Using manual config.mdx instead.');
    return;
  }

  // Read and parse
  console.log(`📖 Reading: ${CONFIG_SERVICE_PATH}`);
  const content = readFileSync(CONFIG_SERVICE_PATH, 'utf-8');

  const entries = parseConfigService(content);
  console.log(`   Found ${entries.length} config entries`);

  if (entries.length === 0) {
    console.warn('⚠️  No entries found - check regex patterns');
    return;
  }

  // Group and generate
  const grouped = groupBySection(entries);
  console.log(`   Grouped into ${grouped.size} sections`);

  const mdx = generateMDX(grouped);

  // Write output
  writeFileSync(OUTPUT_PATH, mdx);
  console.log(`✅ Generated: ${OUTPUT_PATH}`);
  console.log('');
  console.log('Note: The generated file is a reference. The main config.mdx');
  console.log('is manually maintained for better formatting and explanations.');
}

main();
