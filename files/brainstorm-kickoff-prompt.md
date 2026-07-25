# PROMPT KHỞI ĐẦU — Session 0: Brainstorm plugin `dotnet-standards`
# (Copy toàn bộ nội dung dưới dòng này và paste vào Claude Code)

---

## BỐI CẢNH

Tôi là .NET developer. Môi trường của tôi:
- Đã cài plugin **Superpowers** và **superpowers-developing-for-claude-code** (marketplace obra). Superpowers là backbone quy trình (brainstorm → plan → TDD → review). KHÔNG được sửa bất kỳ file nào của Superpowers.
- Đã clone repo tham chiếu **dotnet-claude-kit** (codewithmukesh, MIT license) tại: `./reference/dotnet-claude-kit`. Repo này CHỈ để đọc tham khảo — nó KHÔNG được cài như plugin và không được kích hoạt.
- Repo đích: `./dotnet-standards` — plugin cá nhân tôi sẽ xây, chứa tri thức .NET của tôi (Clean Architecture, MediatR, FluentValidation, AutoMapper, Elasticsearch, Redis...). Repo này sẽ chạy song song với Superpowers như một plugin độc lập.
- Nguồn code exemplar: `./reference/projects/` chứa (một hoặc nhiều) dự án .NET thật của tôi, read-only, gitignored. Đây là mỏ quặng để chưng cất code mẫu trong các session adapt — KHÔNG phải một phần của plugin. Skill thành phẩm phải tự chứa (code exemplar đã chưng cất + sanitize nằm trong thư mục skill), tuyệt đối không trỏ đường dẫn ra dự án thật.

Kiến trúc mục tiêu 3 tầng: Superpowers = tầng quy trình / dotnet-standards = tầng tri thức / CLAUDE.md từng project = chất keo.

## NHIỆM VỤ CỦA SESSION NÀY — VÀ CHỈ SESSION NÀY

Hãy dùng skill **brainstorming** của Superpowers để cùng tôi chốt 4 deliverable sau, ghi vào `./dotnet-standards/docs/`:

1. **`docs/00-brainstorm.md`** — kết quả brainstorm: mục tiêu plugin, phạm vi (in-scope / out-of-scope), danh sách skill mục tiêu với ranh giới rõ ràng cho từng skill (skill nào kích hoạt khi nào, một skill một việc, không có skill "dotnet-everything").
2. **`docs/01-triage-rules.md`** — bộ quy tắc triage (xem QUY TẮC TRIAGE bên dưới, brainstorm để tinh chỉnh chứ không thay thế chúng).
3. **`docs/02-repo-structure.md`** — cấu trúc repo plugin: `.claude-plugin/plugin.json`, `skills/`, `docs/`, cách đăng ký marketplace cá nhân. Dựa trên tri thức từ superpowers-developing-for-claude-code, KHÔNG đoán từ trí nhớ.
4. **`docs/03-session-roadmap.md`** — lộ trình các session tiếp theo: mỗi session đúng MỘT deliverable (ví dụ: S1 = sinh TRIAGE.md; S2 = triage nhanh theo lô nhóm skill kiến thức; S3 = triage nhanh nhóm process; S4+ = mỗi session xử lý sâu 1 thành phần adapt/rebuild/combine). Với mỗi session ghi rõ: input cần đọc, deliverable, định nghĩa "xong".
   Riêng các session adapt, roadmap phải định nghĩa cấu trúc chuẩn 5 bước áp dụng cho mọi session loại này:
   (1) INPUT do tôi cung cấp trong prompt mở session: danh sách đường dẫn file exemplar trong `./reference/projects/` (kèm ví dụ phản diện nếu có — code tôi KHÔNG muốn lặp lại);
   (2) Claude đọc có chủ đích theo ràng buộc 3;
   (3) CHƯNG CẤT: viết lại exemplar vào file tham chiếu của skill — rút gọn còn phần thể hiện pattern, đổi tên domain business thành tên generic, SANITIZE (bỏ connection string, secret, tên package nội bộ, logic nghiệp vụ đặc thù);
   (4) ĐỐI CHIẾU NGƯỢC: kiểm tra quy tắc/checklist trong SKILL.md khớp với code exemplar, không được "nói một đằng code một nẻo";
   (5) Định nghĩa "xong" = skill build được + tôi đã duyệt bản chưng cất + ghi nguồn canonical (dự án/feature nào) vào Decision log của TRIAGE + commit.
   Nếu tôi có nhiều dự án với convention lệch nhau: mỗi skill chọn MỘT dự án làm nguồn canonical (quyết định của tôi), các dự án khác chỉ để đối chiếu; khi lệch nhau, hỏi tôi "từ nay về sau muốn cái nào" chứ không lấy trung bình.

## RÀNG BUỘC CỨNG (không được vi phạm trong bất kỳ tình huống nào)

1. **CẤM implement trong session này.** Không viết skill, không tạo plugin.json thật, không code. Session này chỉ sản xuất 4 file docs ở trên. Nếu tôi (user) lỡ yêu cầu làm thêm, hãy từ chối và ghi yêu cầu đó vào roadmap.
2. **CẤM làm nhiều bước trong một session ở các session sau.** Toàn bộ kế hoạch chạy trên nhiều session/context tách biệt. Một session = một deliverable trong roadmap. Đây là nguyên tắc thiết kế, không phải gợi ý.
3. **Kỷ luật context:** không đọc toàn bộ dotnet-claude-kit. Trong session này chỉ được: liệt kê cây thư mục (`ls`/`tree`), đọc README và tối đa 5 file cụ thể nếu thật sự cần cho việc chốt danh sách skill. Việc đọc sâu để dành cho các session triage.
   Quy tắc này áp dụng CẢ cho `./reference/projects/` ở mọi session: CẤM tự quét toàn bộ dự án của tôi. Chỉ được đọc (a) đúng các file exemplar tôi chỉ đích danh trong prompt mở session, và (b) lookup mở rộng CÓ MỤC TIÊU khi cần hiểu ngữ cảnh (grep/glob theo symbol cụ thể, hoặc Roslyn MCP nếu có) — mỗi lần mở rộng phải nói rõ đang tìm gì và vì sao. Không được tự chọn code làm exemplar thay tôi: codebase thật chứa cả code chuẩn lẫn nợ kỹ thuật, chỉ tôi phân biệt được.
4. **Ngôn ngữ artifact:** mọi file sinh ra (docs, và sau này là skill, description, TRIAGE) viết bằng **tiếng Anh** — để description kích hoạt skill ổn định và tương thích hệ sinh thái. Trao đổi với tôi trong chat bằng tiếng Việt.
5. **Ghi lại commit SHA** của dotnet-claude-kit đang tham chiếu (chạy `git -C ./reference/dotnet-claude-kit rev-parse HEAD`) vào đầu `docs/00-brainstorm.md` — kit này cập nhật nhanh, mọi quyết định triage phải neo vào một phiên bản cố định.
6. **Kết thúc session:** (a) commit toàn bộ docs với message rõ ràng; (b) sinh file `docs/next-session-prompt.md` chứa prompt khởi động hoàn chỉnh cho session kế tiếp (bối cảnh tối thiểu + file cần đọc + deliverable duy nhất + nhắc lại ràng buộc 2 và 3). Từ session sau trở đi, mỗi session cũng kết thúc bằng việc cập nhật lại file này.

## QUY TẮC TRIAGE (input cho brainstorm — tinh chỉnh, không thay thế)

Trạng thái triage có 5 + 1 giá trị: `keep` / `keep-tweak` / `adapt` / `rebuild` / `skip` / `combine` (combine chỉ dành cho nhóm process).

**Nhóm A — Skill kiến thức thuần** (ef-core, minimal-api, caching, clean-architecture, serilog, resilience...):
- Nếu tôi ĐÃ có code mẫu/convention riêng → `adapt`: giữ khung skill của kit, thay ruột bằng code thật và convention của tôi.
- Nếu tôi CHƯA có code mẫu → đề xuất `keep` hoặc `keep-tweak` kèm nhận xét chất lượng; các skill này có thể nâng cấp lên `adapt` sau khi tôi có code mẫu (ghi chú "upgrade candidate" trong TRIAGE).

**Nhóm B — Agents, workflow commands, meta-skills, hooks (tầng quy trình):**
- KHÔNG mặc định skip. Với TỪNG thành phần, phải so sánh với chức năng tương ứng của Superpowers và đề xuất một trong ba: `skip` (Superpowers đã làm tốt hơn/tương đương) / `keep` (Superpowers không có mà tôi cần) / `combine` (Superpowers có nhưng còn base, phần của kit bổ sung được).
- Mọi đề xuất `keep`/`combine` cho nhóm B BẮT BUỘC kèm **conflict check**: hook có bắn cùng event với hook của Superpowers không? slash command có trùng/na ná tên không? instruction có mâu thuẫn với quy trình brainstorm→plan→TDD→review của Superpowers không? Nếu có xung đột mà không hóa giải được → hạ xuống `skip`.
- Quy tắc vàng của `combine`: phần mở rộng LUÔN nằm trong plugin dotnet-standards của tôi dưới dạng skill/hook mới; TUYỆT ĐỐI không sửa file của Superpowers.

**Nhóm C — Roslyn MCP server (CWM.RoslynNavigator):** mặc định `keep` như công cụ ngoài (dotnet tool cài riêng), không chép vào plugin, không xung đột với ai.

**Nhóm D — Rules, project templates:** đánh giá từng cái; rules phù hợp thì chuyển thành nội dung trong skill hoặc mẫu CLAUDE.md cấp project (tầng 3), không nhất thiết giữ nguyên cơ chế rules của kit.

## CÁCH LÀM VIỆC VỚI TÔI TRONG SESSION NÀY

- Brainstorm là hội thoại: hỏi tôi từng câu một về phạm vi, stack, taste (đừng dồn 10 câu một lượt). Ưu tiên hỏi những quyết định chỉ tôi trả lời được: skill nào tôi dùng hằng ngày, kiến trúc chuẩn của tôi là gì, cái gì out-of-scope.
- Khi chốt danh sách skill, sắp theo thứ tự ưu tiên triển khai: skill tôi dùng nhiều nhất làm trước.
- Cuối session, tóm tắt lại các quyết định và những câu hỏi còn treo (ghi vào docs, không để trong chat).

Bắt đầu: xác nhận bạn đã hiểu ràng buộc, đọc cây thư mục của `./reference/dotnet-claude-kit`, rồi đặt câu hỏi brainstorm đầu tiên.
