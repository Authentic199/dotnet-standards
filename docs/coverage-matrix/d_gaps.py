import io, re, json, os

KNOWLEDGE = ["api-surface","auth-and-security","automapper-mapping","common-extensions",
"distributed-caching","distributed-lock","dotnet-testing","ef-core-data-access",
"elasticsearch-search","error-handling","excel-miniexcel","facade-module-architecture",
"file-storage","http-client-factory","list-query-pipeline","mediatr-messaging",
"message-keys","module-feature"]

find_tokens = set(json.load(io.open("d_findtokens.json", encoding="utf-8")))
# normalise: a Find token covers a rule token if the rule token appears inside it
def covered(tok):
    if tok in find_tokens: return True
    for ft in find_tokens:
        if tok in ft: return True
    return False

NORM = re.compile(r"\b(never|always|must|is a defect|is the defect|do not|don't|"
                  r"is wrong|is banned|forbidden|no other|not allowed|is never|"
                  r"only ever|exactly one|is a bug)\b", re.I)
# a token that could anchor a grep: has a capital or a paren or a dot, no spaces
CODEY = re.compile(r"^[A-Za-z_@\[\]<>\.\(\)\{\}!\?:$#/\-]+$")

rows = []
for s in KNOWLEDGE:
    for path in [ "skills/%s/SKILL.md" % s ] + \
        [ "skills/%s/references/%s" % (s,f) for f in sorted(os.listdir("skills/%s/references" % s)) ] \
        if os.path.isdir("skills/%s/references" % s) else [ "skills/%s/SKILL.md" % s ]:
        if not os.path.exists(path): continue
        t = io.open(path, encoding="utf-8").read()
        t = re.sub(r"```.*?```", "", t, flags=re.S)          # drop code blocks
        for para in re.split(r"\n\s*\n", t):
            flat = " ".join(para.split())
            if not NORM.search(flat): continue
            toks = [x for x in re.findall(r"`([^`\n]+)`", flat)
                    if 2 < len(x) < 42 and CODEY.match(x)]
            if not toks: continue
            if any(covered(x) for x in toks): continue
            rows.append((s, os.path.basename(path), toks[:4], flat[:150]))

print("CANDIDATE UNCOVERED RULES: %d\n" % len(rows))
from collections import Counter
for s, n in Counter(r[0] for r in rows).most_common():
    print("  %-30s %d" % (s, n))
io.open("d_candidates.json","w",encoding="utf-8").write(
    json.dumps([{"skill":a,"file":b,"tokens":c,"text":d} for a,b,c,d in rows], ensure_ascii=False, indent=1))
