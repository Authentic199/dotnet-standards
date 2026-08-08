import io, json, re
rows = json.load(io.open("d_candidates.json", encoding="utf-8"))
# keep only paragraphs whose rule is stated in the house's bold-imperative form
strong = [r for r in rows if r["text"].lstrip().startswith("**") or "**" in r["text"][:90]]
print("STRONG (bold-imperative form): %d of %d\n" % (len(strong), len(rows)))
for r in strong:
    print("[%s / %s]" % (r["skill"], r["file"]))
    print("   tokens: %s" % ", ".join(r["tokens"]))
    print("   %s" % r["text"][:145].replace("**",""))
io.open("d_strong.json","w",encoding="utf-8").write(json.dumps(strong, ensure_ascii=False, indent=1))
