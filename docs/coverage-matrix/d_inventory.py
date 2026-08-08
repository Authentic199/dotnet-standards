import io, re, os, json

FILES = []
for s in ["dotnet-code-review","dotnet-architecture-review","dotnet-security-review","dotnet-performance-review"]:
    FILES.append(("skills/%s/SKILL.md" % s, s))
    d = "skills/%s/references" % s
    if os.path.isdir(d):
        for f in sorted(os.listdir(d)):
            FILES.append(("%s/%s" % (d, f), s))

checks = []
# bold form:  **1.12 Title** — *SEV* · owner
bold = re.compile(r"^\*\*(\d+\.\d+)\s+(.+?)\*\*", re.M)
# table form: | 1.5 | **Title.** ...
tbl  = re.compile(r"^\|\s*(\d+\.\d+)\s*\|\s*\*\*(.+?)\.\*\*", re.M)

for path, skill in FILES:
    t = io.open(path, encoding="utf-8").read()
    for m in bold.finditer(t):
        checks.append((skill, os.path.basename(path), m.group(1), m.group(2)[:70]))
    for m in tbl.finditer(t):
        checks.append((skill, os.path.basename(path), m.group(1), m.group(2)[:70]))

print("TOTAL CHECKS: %d\n" % len(checks))
from collections import Counter
c = Counter((s, f) for s, f, _, _ in checks)
for (s, f), n in sorted(c.items()):
    print("  %-28s %-26s %d" % (s, f, n))

# collect every token that appears inside a Find: grep across all rubric files
find_tokens = set()
for path, skill in FILES:
    t = io.open(path, encoding="utf-8").read()
    for m in re.finditer(r"`Find:`(.{0,700}?)(?=\n\n|\Z)", t, re.S):
        for tok in re.findall(r"`([^`\n]+)`", m.group(1)):
            find_tokens.add(tok)
        # also bare greps written without backticks
        for tok in re.findall(r'grep[^\n]*?"([^"\n]+)"', m.group(1)):
            find_tokens.add(tok)
io.open("d_findtokens.json","w",encoding="utf-8").write(json.dumps(sorted(find_tokens), ensure_ascii=False, indent=0))
print("\nDistinct tokens appearing in Find: instructions: %d" % len(find_tokens))
