# Quick Start

## Install

```bash
dotnet tool install --global Sherlock.Net.Cli
```

## Search

```bash
# Single username
sherlock johndoe

# Multiple usernames
sherlock --csv johndoe janedoe

# Specific sites only
sherlock --site GitHub --site Twitter johndoe

# With proxy/Tor
sherlock --proxy socks5://127.0.0.1:9050 johndoe

# Show all results (including not found)
sherlock --print-all johndoe
```

## Export results

```bash
sherlock --txt johndoe          # user123.txt
sherlock --csv johndoe          # user123.csv
sherlock --json-export johndoe  # user123.json
sherlock --csv -o results/ johndoe janedoe
```

## Username wildcards

Use `{?}` to try separator variants (`_`, `-`, `.`):

```bash
sherlock john{?}doe
# Searches: john_doe, john-doe, john.doe
```
