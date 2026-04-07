# CLI Reference

## Usage

```
sherlock <USERNAMES> [OPTIONS]
```

## Arguments

| Argument | Description |
|---|---|
| `<USERNAMES>` | One or more usernames to search for |

## Options

| Option | Default | Description |
|---|---|---|
| `-h, --help` | | Prints help information |
| `-v, --version` | | Prints version information |
| `--timeout <SECONDS>` | `60` | Time in seconds to wait for response |
| `--proxy <URL>` | | Proxy URL (e.g., `socks5://127.0.0.1:9050` for Tor) |
| `--site <NAME>` | | Limit search to specific site(s). Can be repeated. |
| `--json <PATH_OR_URL>` | | Custom data.json file path or URL |
| `--csv` | | Export results as CSV |
| `--txt` | | Export results as TXT |
| `--json-export` | | Export results as JSON |
| `-o, --output <DIR>` | | Output directory for export files |
| `--print-all` | | Show all results, not just found accounts |
| `--nsfw` | | Include NSFW sites in search |
| `-b, --browse` | | Open found URLs in default browser |
| `--no-color` | | Disable colored output |
| `--concurrency <COUNT>` | `20` | Maximum concurrent requests |

## Examples

```bash
# Basic search
sherlock johndoe

# Search specific sites with timeout
sherlock --site GitHub --site Reddit --timeout 30 johndoe

# Export to CSV in custom directory
sherlock --csv -o ~/results johndoe janedoe

# Anonymous search through Tor
sherlock --proxy socks5://127.0.0.1:9050 johndoe

# Use custom site database
sherlock --json https://example.com/custom-data.json johndoe

# Show everything including not found and errors
sherlock --print-all --no-color johndoe > output.txt
```
