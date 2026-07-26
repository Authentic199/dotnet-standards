# 04 — Danh mục những gì đã bị bỏ qua (triage)

> **Mục đích.** Một nơi duy nhất để xem lại **mọi thứ đã bị loại** trong quá trình triage
> `dotnet-claude-kit` → `dotnet-standards`, kèm lý do bằng tiếng Việt dễ hiểu. Dùng khi sau này
> muốn mở rộng plugin và cần biết: *cái này đã từng cân nhắc chưa? bỏ vì sao? mở lại được không?*
>
> **Đây là tài liệu dẫn xuất, không phải nguồn sự thật.** Nguồn sự thật là
> [`TRIAGE.md`](TRIAGE.md) — mỗi mục dưới đây đều ghi mã row để tra ngược. Nếu hai file mâu thuẫn,
> **`TRIAGE.md` đúng**. Quy tắc phân loại nằm ở [`01-triage-rules.md`](01-triage-rules.md), phạm vi
> dự án nằm ở [`00-brainstorm.md`](00-brainstorm.md) §2.
>
> Trạng thái tại thời điểm viết: triage đã đóng, **94/94 row đã quyết định** (S5).
> Kit được ghim tại commit `cd83d315986c27621da178dad73bd95d503c1540`.

---

## 1. Tổng quan bằng số

| | Tổng row | Bị loại | Tỉ lệ |
|---|---|---|---|
| Nhóm A — kỹ năng kiến thức | 33 | **11** | 33% |
| Nhóm B — tầng quy trình | 33 | **18** | 55% |
| Nhóm C — MCP server | 1 | 0 | 0% |
| Nhóm D — luật, kiến thức, template | 27 | **12** | 44% |
| **Tổng** | **94** | **41** | **44%** |

Ngoài ra có **1 thành phần không đánh số** cũng bị loại: `mcp-configs/` (mã `C‑u1`).

**Điều quan trọng nhất cần hiểu về con số 41 này:** phần lớn **không phải là mất kiến thức**.
Rất nhiều row bị `skip` vì nội dung của nó **đã được một row khác ship rồi** — bỏ để tránh trùng
lặp, không phải để vứt đi. Xem §7 "Kho vật liệu đã cứu" để thấy những gì được giữ lại dù row bị
loại.

### Phân biệt hai loại `skip` — rất quan trọng

| Loại | Số lượng | Nghĩa là gì |
|---|---|---|
| **R4 — cắt ngắn** | 7 | Chủ đề nằm trong bảng loại trừ v1. **Không đọc file, loại ngay.** |
| **Có lý do** | 34 | Chủ đề *nằm trong* phạm vi, nhưng thành phần cụ thể này không dùng được. |

Nhầm hai loại này là nguy hiểm: một cái nói *"chủ đề đã chết"*, cái kia nói *"chủ đề còn sống,
chỉ là món này không xài được"*. Mọi mục dưới đây đều ghi rõ nó thuộc loại nào.

---

## 2. Bảy nhóm nguyên nhân

Toàn bộ 41 row bị loại rơi vào đúng bảy nhóm. Đây là cách nhanh nhất để định vị:

*(Bảng đếm 41 row **có số**. `C‑u1` không đánh số nên nằm ngoài, xếp vào nhóm 4.)*

| # | Nhóm nguyên nhân | Số row | Mở lại được không? |
|---|---|---|---|
| 1 | **Ngoài phạm vi v1 (R4)** — Blazor, Docker, CI/CD, Aspire, modular monolith | 7 | Chỉ khi đổi phạm vi dự án |
| 2 | **Đụng Q1** — quy định kiến trúc, mà kiến trúc thật của bạn chưa đặt tên | 7 | Có — sau khi S7 chốt Q1 |
| 3 | **Mâu thuẫn stack** — kit khuyên ngược lại thứ bạn đang dùng | 6 | Chỉ khi bạn đổi stack |
| 4 | **Đã có nơi khác ship** — trùng nội dung với row còn sống | 5 | Không cần — nội dung vẫn còn |
| 5 | **Xung đột cơ chế không gỡ được (R5)** | 5 | Khó — vấn đề nằm ở cơ chế |
| 6 | **Sai tầng** — là quy trình, không phải kiến thức .NET | 3 | Không nên — sai kiến trúc 3 tầng |
| 7 | **Hook: chi phí > lợi ích** | 4 | Có, nếu điều kiện Windows đổi |
| — | **Hoãn theo Q6** (không hẳn là loại) | 4 | **Có — đã lên lịch sau v1** |

> Ghi chú về độ chính xác: bảng phân nhóm này là cách sắp xếp của *tài liệu này*, hơi khác với ba
> phân nhóm mà nhật ký S4 dùng cho riêng nhóm B. Danh sách S4 đó bỏ sót **B27** và **B33** —
> ở đây cả hai đều được xếp nhóm đầy đủ (nhóm 7 và nhóm 4).

---

## 3. Nhóm 1 — Ngoài phạm vi v1 (R4): 7 row

Đây là những row bị loại **trước khi đọc**, theo quy tắc R4. Chủ đề nằm trong bảng loại trừ ở
`00-brainstorm.md` §2. Mục đích của R4 là **tiết kiệm ngân sách ngữ cảnh** cho các phiên triage.

| Mã | Thành phần | Chủ đề bị loại trừ |
|---|---|---|
| A04 | `skills/aspire/` | .NET Aspire — "không dùng" |
| A07 | `skills/ci-cd/` | CI/CD — "người khác lo, bạn chọn không tham gia" |
| A11 | `skills/container-publish/` | Xuất bản container |
| A15 | `skills/docker/` | Docker |
| B16 | `agents/devops-engineer.md` | Docker + CI/CD + Aspire gộp lại |
| D23 | `templates/blazor-app/` | Blazor / frontend .NET |
| D25 | `templates/modular-monolith/` | Modular monolith |

**Điều kiện mở lại:** chỉ khi bạn thay đổi bảng phạm vi ở `00-brainstorm.md` §2. Không có gì cần
cân nhắc lại ở cấp độ từng row — chúng chưa từng được đọc.

**Một lưu ý:** phần health check của Docker (A15) **không bị mất** — health check endpoint của
ASP.NET nằm ở A21, và A21 được giữ.

---

## 4. Nhóm 2 — Đụng Q1 (kiến trúc chưa đặt tên): 7 row

**Bối cảnh Q1:** kiến trúc thật của các dự án bạn **không phải Clean Architecture**, và nó **cố ý
chưa được đặt tên** cho tới phiên S7. Bất kỳ thành phần nào *quy định* một cách phân tầng, hoặc
*bắt chọn* giữa các kiến trúc, đều sẽ trả lời hộ Q1 — nên không thể thừa hưởng.

**Nguyên nhân gốc:** kit được thiết kế **đa kiến trúc có chủ đích** — nó hỗ trợ 4 kiến trúc
(VSA, Clean, DDD+Clean, Modular Monolith) như nhau, và có hẳn một "cố vấn" hỏi bạn chọn cái nào.
`dotnet-standards` thì ngược lại: mã hoá **đúng một** kiến trúc — của bạn. Đây là **nguyên nhân
đơn lẻ gây ra nhiều skip nhất trong toàn dự án.**

| Mã | Thành phần | Vì sao loại |
|---|---|---|
| A03 | `skills/architecture-advisor/` | Bộ chọn 4 kiến trúc. Plugin chỉ mã hoá 1 kiến trúc ⇒ bộ chọn **không có người dùng**. |
| A08 | `skills/clean-architecture/` | Kê rõ 4 project Domain/Application/Infrastructure/Api. Giữ là trả lời hộ Q1. |
| A35 | `skills/vertical-slice/` | Hai nửa đều không dùng được: nửa handler dành cho Mediator/Wolverine (bạn dùng MediatR), nửa bố cục thư mục là địa hạt Q1. |
| B17 | `agents/dotnet-architect.md` | Bước 1 của nó là *"luôn nạp architecture-advisor trước"* ⇒ **mở lại Q1 mỗi lần gọi**. |
| D02 | `.claude/rules/architecture.md` | File lệch nhất nhóm D. Xem chi tiết bên dưới. |
| D17 | ADR-001 "VSA làm mặc định" | Quy định VSA. **Và chính tác giả kit đã rút lại** — trạng thái ghi "Superseded by ADR-005". |
| D21 | ADR-005 "hỗ trợ đa kiến trúc" | Chính là văn bản tuyên bố cái tiền đề gây ra tất cả những skip trên. |

### D02 đáng nói riêng — cả 6 mục đều hỏng

Đây là file lệch nhất trong nhóm D, và không mục nào cần phải cân nhắc:

1. *"Đừng bao giờ tự đoán kiến trúc — dùng skill architecture-advisor"* → A03 đã bị loại, con trỏ chết.
2. *"Thư mục theo tính năng thay vì theo tầng"* — và ví dụ **phản diện** của nó vẽ đúng
   `Controllers/` + `Services/`. **Stack thật của bạn chính là ví dụ phản diện của file này.**
3. *"Mỗi nhóm endpoint một file, implement `IEndpointGroup`"* + *"đừng bao giờ thêm `MapGroup` vào
   `Program.cs`"* → đây chính là luật khiến kit **cấm dùng Controllers**.
4. *"Chiều phụ thuộc hướng vào trong: Domain → Application → Infrastructure → Presentation"* →
   quy định phân tầng ⇒ đụng Q1.
5. *"Không dùng repository pattern trên EF Core"* → ý kiến chưa ngã ngũ, xem §8.
6. Ranh giới module + shared kernel → khung Modular Monolith, thuộc vùng R4.

**Không mất gì:** ý tưởng duy nhất trung lập với kiến trúc — *phải kiểm tra chiều phụ thuộc, không
được đảo mũi tên* — đã được **A02** giữ dưới dạng **phương pháp** trong rubric kiểm tra kiến trúc.

**Điều kiện mở lại:** sau khi **S7 chốt Q1**. Nếu kiến trúc của bạn hoá ra gần với một trong bốn
loại kit hỗ trợ, A08 hoặc A35 có thể được mở lại — nhưng đọc lại từ SHA đã ghim, đừng dựa vào tóm
tắt ở đây.

---

## 5. Nhóm 3 — Mâu thuẫn stack: 6 row

Kit khuyên ngược lại thứ bạn đang dùng thật. Giữ lại thì mỗi lần gọi nó lại đẩy bạn ra khỏi stack
của chính mình.

**Stack đã chốt (`00-brainstorm.md` §2):** Controllers (MVC) · Swagger UI / Swashbuckle ·
không versioning · MediatR + FluentValidation + AutoMapper · Redis · Elasticsearch.

| Mã | Thành phần | Kit khuyên | Bạn dùng |
|---|---|---|---|
| A22 | `skills/messaging/` | Wolverine / MassTransit, outbox, saga, RabbitMQ | **Không có message broker nào** |
| A27 | `skills/project-setup/` | 4/8 mặc định trái với bạn | HybridCache→Redis, Scalar→Swashbuckle, Wolverine→MediatR, messaging "None" |
| A31 | `skills/scalar/` | Scalar làm UI tài liệu API | **Swagger UI / Swashbuckle** |
| B13 | `agents/api-designer.md` | Chuyên gia thiết kế **Minimal API** | **Controllers** |
| D15 | `mediatr-to-mediator-migration.md` | Hướng dẫn **rời bỏ** MediatR | **Ở lại MediatR** |
| D20 | ADR-004 "HybridCache" | *"Tránh IDistributedCache thủ công"* | **Redis cache-aside thủ công** |

### Vì sao B13 nguy hiểm hơn vẻ ngoài

Row của nó ghi rất thẳng: *"nó không mâu thuẫn với **quy trình**, nó mâu thuẫn với **stack** — và
thế còn tệ hơn"*. Một agent mâu thuẫn quy trình thì gây khó chịu; một agent mâu thuẫn stack thì
**đẻ ra code Minimal API vào codebase Controllers mỗi lần được gọi**.

### Quyết định MediatR đã giết hoặc viết lại 5 row

Đây là quyết định có sức lan toả lớn nhất sau Q1. Bạn **ở lại MediatR**, không chuyển sang Mediator
hay Wolverine. Hệ quả: **A27** (Nguyên tắc số 3 của nó lập luận phải bỏ MediatR), **A35** (skeleton
dành cho Mediator), **một phần B05** và **một phần B06** (bảng "giải pháp thay thế miễn phí" khuyên
di cư), **D15** (toàn bộ file) — cộng với việc nó định hình quyết định `rebuild` cho
`cqrs-feature-slice`.

### A22 — điểm dễ hiểu nhầm

A22 **không phải** R4. Background worker **nằm trong phạm vi** (§2 liệt kê Worker service là 1 trong
2 hình dạng dự án). Vấn đề là skill này **không hề chứa nội dung `BackgroundService` nào** — toàn bộ
ruột của nó là message broker. Hệ quả: cụm `background-worker` không thừa hưởng được gì từ nhóm A.

**Điều kiện mở lại:**
- A22 → khi có message broker vào stack. **Ý tưởng transactional outbox** đáng nhặt lại lúc đó.
- A31 → khi bạn chuyển sang Scalar.
- D15/A27/B13 → chỉ khi đổi stack cốt lõi (MediatR hoặc Controllers).
- D20 → gắn với quyết định HybridCache-vs-Redis mà S7 nợ, xem §8.

---

## 6. Nhóm 4→7 — Bốn nhóm còn lại

### Nhóm 4 — Đã có nơi khác ship (5 row): *không mất gì*

Nội dung vẫn còn nguyên, chỉ là nằm ở row khác. Bỏ để **tránh ship trùng hai lần**.

| Mã | Thành phần | Nội dung của nó giờ nằm ở đâu |
|---|---|---|
| B15 | `agents/code-reviewer.md` | **A09** — rubric `dotnet-code-review` |
| B20 | `agents/refactor-cleaner.md` | **A13** — danh mục "code slop" trong `dotnet-code-review` |
| B21 | `agents/security-auditor.md` | **A32** — checklist 10 mục của nó là *tập con* của 6 lớp trong A32 |
| B22 | `agents/test-engineer.md` | **A34 + B09** — cụm `dotnet-testing` |
| B33 | `skills/health-check/` | Không có nơi nào — xem bên dưới |

**B22 bị loại vì một lý do đáng nhớ, không phải vì trùng lặp.** Nó **không xung đột với gì cả**.
Nó trượt "phép thử ngữ cảnh": agent chỉ đáng có khi nó *"đi đường dài, trả về ngắn gọn"*. Viết test
**không phải** đường dài — vì **sản phẩm chính là code**, phải đổ về ngữ cảnh chính dù sao đi nữa.

**B33 là ca đặc biệt — bị kẹt giữa hai nhóm.** Dạng "điều phối viên" của nó vi phạm §5 (tạo quy
trình review thứ hai). Nhưng bóc lớp điều phối ra thì phần còn lại là *kiến thức chấm điểm*, mà
kiến thức đó **trải khắp cả 4 lăng kính review** nên **không có một đích đến duy nhất** — vi phạm
R2. Không ship được ở nhóm A, dạng ship được duy nhất ở nhóm B thì §5 cấm. **Vật liệu của nó được
cứu bắt buộc**, xem §7.

### Nhóm 5 — Xung đột cơ chế không gỡ được (5 row)

Không phải nội dung dở — mà **cơ chế** của nó đụng vào thứ môi trường này đã chạy.

| Mã | Thành phần | Xung đột |
|---|---|---|
| B04 | `skills/instinct-system/` | Ghi **nội dung luật thẳng vào `MEMORY.md`**, trong khi quy ước bộ nhớ của môi trường quy định `MEMORY.md` là **mục lục một dòng, không bao giờ chứa nội dung**. Nó còn thêm 2 kho nữa ⇒ 3 hệ thống bộ nhớ cạnh tranh. Lỗi phụ: thang tin cậy **âm thầm áp dụng giả thuyết chưa xác nhận từ mức 0.7**. |
| B12 | `skills/wrap-up/` | Đổ theo B04 — nó là "cửa trước" cuối phiên của B04. Nửa còn lại là quy trình chung, không có chất .NET. |
| B07 | `skills/plan/` | Bước 2 là bộ chọn kiến trúc ⇒ mở lại Q1 mỗi lần lập kế hoạch. Bỏ bước 2 thì chỉ còn template kế hoạch chung mà `writing-plans` đã lo, kèm mô tả tranh giành định tuyến. |
| B08 | `skills/spec/` | Trùng bề mặt mô tả với `brainstorming`. **Nhưng checklist của nó được cứu** — xem §7. |
| B11 | `skills/workflow-mastery/` | Trùng 4 skill Superpowers. **Hai mảnh được cứu** — xem §7. |

> **Ghi chú kỹ thuật quan trọng:** Superpowers **không đăng ký agent nào và không có slash command
> nào**. Nghĩa là **không row nào bị loại vì trùng tên**. Mọi xung đột ghi nhận đều là **trùng bề
> mặt mô tả** — tức là Claude sẽ chọn nhầm skill, chứ không phải hệ thống báo lỗi.

### Nhóm 6 — Sai tầng: là quy trình, không phải kiến thức .NET (3 row)

Kiến trúc 3 tầng: **Superpowers = quy trình · `dotnet-standards` = kiến thức · `CLAUDE.md` = keo dán.**
Ba row này là quy trình thuần tuý, **không có chất .NET nào**. Giữ chúng sẽ biến plugin kiến thức
.NET thành chủ sở hữu của quy trình git — đảo ngược đúng cái kiến trúc mà cả dự án dựa vào.

| Mã | Thành phần | Nội dung |
|---|---|---|
| B02 | `skills/checkpoint/` | Commit giữa phiên + ghi chú bàn giao. Ngôn ngữ-bất-kỳ. |
| D05 | `.claude/rules/git-workflow.md` | Conventional commit, đặt tên nhánh, commit nguyên tử, không force-push main. |
| D22 | `knowledge/decisions/template.md` | Mẫu ADR trắng, 34 dòng tiêu đề. |

**D22 là row duy nhất trong plugin bị loại vì hoàn toàn không chứa nội dung .NET nào** — nó không
xung đột với bất cứ thứ gì, đó cũng chính là lý do nó không có chỗ đứng.

**Không nên mở lại.** Mở lại là phá kiến trúc 3 tầng.

### Nhóm 7 — Hook: chi phí lớn hơn lợi ích (4 row)

**Bối cảnh Windows:** Claude Code chạy hook qua `CMD.exe`, không chạy được `.sh`. Giữ bất kỳ hook
nào cũng phải kèm bộ bọc `run-hook.cmd` và **phụ thuộc Git for Windows**. Bộ bọc đó **thoát 0 im
lặng khi không tìm thấy bash** — hook đơn giản là không chạy và không ai biết.

**Quy tắc rút ra từ Q2:** *một hook chỉ được ship trên Windows nếu việc nó vắng mặt trong im lặng
là vô hại theo thiết kế.*

| Mã | Thành phần | Vì sao loại |
|---|---|---|
| B27 | `pre-bash-guard.sh` | **Đây là row quyết định quy tắc trên.** Nếu không có bash, cái chắn này **hỏng theo hướng mở**: nó ngừng chặn, không báo gì, còn bạn vẫn tin force-push đang được canh. **Một cơ chế an toàn không đáng tin còn tệ hơn không có.** |
| B25 | `post-scaffold-restore.sh` | Chạy `dotnet restore` **toàn solution, đồng bộ, vô điều kiện** mỗi lần sửa `.csproj` — trong khi `dotnet build` và `dotnet add package` đều đã tự restore. Luật của chính kit phải thêm dòng bảo người dùng *chờ* nó. |
| B26 | `post-test-analyze.sh` | Tóm tắt kết quả test bằng shell — thứ Claude tự làm miễn phí và đầy đủ ngữ cảnh hơn. Header của chính nó ghi logic đếm **đã sai hai lần**. |
| B30 | `pre-commit-format.sh` | Lớp thứ ba trên cùng một mối lo. Cùng **y hệt một lệnh** với B10 giai đoạn 6, sau khi B24 đã ngăn từ lúc sửa file. Thô nhất trong ba: chạy toàn solution thay vì chỉ project bị đổi. |

**Kết quả:** plugin ship **đúng 1 hook** — `post-edit-format` (B24) — vì với nó, vắng mặt trong im
lặng là **vô hại**: code không được format nhưng vẫn đúng, và B10 giai đoạn 6 bắt được độ lệch
trước khi review.

**Điều kiện mở lại:** nếu môi trường Windows thay đổi (ví dụ Claude Code chạy `.sh` trực tiếp), thì
B27 đáng xem lại đầu tiên — nó bị loại vì *hướng hỏng*, không phải vì nội dung.

---

## 7. Nhóm 8 — Hoãn theo Q6: 4 row *(không hẳn là loại)*

**Đây là nhóm bạn nên xem đầu tiên khi muốn phát triển tiếp.** Q6 quyết định **không ship template
`CLAUDE.md` cho từng dự án trong v1** — nhưng đó là **hoãn lại, không phải loại trừ**.

| Mã | Thành phần | Trạng thái | Chi phí thu hoạch sau này |
|---|---|---|---|
| A16 | `skills/dotnet-init/` | skip | Bộ khung phụ thuộc vào A03 (chết) và D23–D27. Còn lại quá mỏng. |
| D24 | `templates/class-library/` | skip → **drop** | **Không** nằm trong 2 hình dạng dự án của §2 ⇒ không đủ điều kiện vào cả backlog. |
| **D26** | `templates/web-api/` | skip → **project `CLAUDE.md` material** | Cần **viết lại cho Controllers**: đếm được `IEndpointGroup` ×2, `TypedResults` ×2, `MapGroup`, "Minimal API", Wolverine — và **0 lần** nhắc Controllers. |
| **D27** | `templates/worker-service/` | skip → **project `CLAUDE.md` material** | Ô nhiễm dạng broker: **Wolverine ×5**. Nhưng sạch phần transport. Đáng thu hoạch hơn D26. |

**Cách đọc D26/D27 cho đúng:** hai cột phải đọc cùng nhau —
**`skip` = không mang vào v1** · **`project CLAUDE.md material` = đã ghi nhận cho tầng 3, thu hoạch
khi Q6 mở lại sau v1.** Không ship gì cả, và cũng không mất gì cả. Đây là **hai row duy nhất trong
toàn plugin** dùng giá trị đích này.

---

## 8. Kho vật liệu đã cứu — *phần hữu ích nhất khi muốn phát triển tiếp*

Những row dưới đây **bị loại, nhưng vật liệu bên trong được giữ lại trong nhật ký quyết định** để
cái `skip` không phá huỷ chúng. Đây là danh sách "đào lên dùng được".

Quy ước phân biệt (chốt ở S4): một row là `combine` khi **vật liệu có tên đi tới đích có tên**; là
`skip` + ghi nhật ký khi **vật liệu có thật nhưng không có một đích đến duy nhất**.

| Nguồn | Vật liệu được cứu | Ai nên dùng |
|---|---|---|
| **A08** | 4 khối phản-mẫu trung lập kiến trúc: **anemic domain model** · **DbContext trong tầng Domain** · **fat endpoint** · **repository-per-entity** | R8 cho `solution-architecture` và rubric kiến trúc — S7 |
| **A22** | Khái niệm **transactional outbox** | Chỉ khi có broker vào stack |
| **B08** | **9 chiều đặt câu hỏi**. Ba chiều là câu hỏi web-API .NET mà brainstorm ngôn-ngữ-bất-kỳ không nghĩ tới: **hợp đồng API** (tài nguyên, hình dạng request/response, phân trang, versioning) · **phân quyền** (mô hình role/claim, ranh giới tenant) · **tích hợp** (dịch vụ ngoài, event phát ra, webhook, side effect). Cộng tiêu chí chấp nhận dạng Given/When/Then | Có thể thành checklist `references/` cho `superpowers:brainstorming` — S7 |
| **B08** | Hai câu đáng giữ nguyên văn: *"nếu Claude bắt gặp mình đang nghĩ 'chắc là' hoặc 'kiểu thông thường', ý nghĩ đó là một câu hỏi phải hỏi, không phải một quyết định được tự đưa ra"* và *"'tôi không biết' là câu trả lời hợp lệ — nó chuyển sang mục Quyết định hoãn kèm phương án dự phòng chọn ngay bây giờ; hoãn trong im lặng là bị cấm"* | — |
| **B11** | **Danh sách cho phép `dotnet`** cho `.claude/settings.json`: `Bash(dotnet build *)`, `test`, `run`, `ef`, `format`, `restore`, `pack`, `tool` | `project-scaffolding` — đã lên lịch cho S7, ghép với D06 |
| **B11** | **Kinh tế token .NET** — con số định lượng cho quyết định C01 | ✅ **Đã dùng** — C01 giữ lại nhờ nó |
| **B12** | Dò tìm `.slnx`/`.sln` lúc khởi động | ❌ **Không cần nữa** — S5 phát hiện server tự làm rồi, xem §9 |
| **B33** | **Cổng phân loại bước 2.5** và file `references/grading-rubric.md` | **Bắt buộc thu hoạch ở S7**, không bị ảnh hưởng bởi `skip` |
| **D20** | 4 kiểu hỏng của cache-aside tự viết: **cache stampede** · **boilerplate serialization** · **lệch thời hạn theo từng lời gọi** · **không có invalidation theo tag**. Cộng phần *"khi nào KHÔNG nên dùng HybridCache"* | R8 nhắm vào **chính code Redis của bạn** — gắn với quyết định S7 nợ |
| **B15 / B20 / B21** | Ghi chú một dòng | Đã ghi trong nhật ký |

---

## 9. Những câu hỏi còn mở mà các row bị loại chạm vào

Không có row nào được phép tự quyết những việc này. Ghi lại để sau này biết chỗ tìm.

**① Repository trên EF Core — 4 nguồn mâu thuẫn nhau, 2 trong đó nằm bên trong chính kit.**
- **A12** kê `repository-per-aggregate-root` (tức là **nên** dùng)
- **A17** đánh dấu lập trường ngược lại của kit là "ý kiến cần xác nhận với code thật"
- **D02** phát biểu *"không dùng repository trên EF Core"* thành **luật**
- **D19** lập luận nó thành **quyết định**
- **A29** thì lại có mẫu inject vào `OrderRepository`, đụng chính luật của kit

⇒ **Kit tự mâu thuẫn**, nên không có lập trường nào để thừa hưởng dù muốn. Code thật của bạn có
tầng `Facades/` — chưa ai mở ra xem đó có phải wrapper kiểu repository không. **Nợ S7.**

**② HybridCache vs Redis — 4 row, một quyết định duy nhất giải quyết cả bốn.**
A06 (ghi nhận lệch) · B19 (mặc định HybridCache) · D08 (phát biểu thành luật) · D20 (phát biểu
thành ADR, bị loại). S7 quyết một lần: theo HybridCache, hay giữ Redis cache-aside và khắc phục
4 kiểu hỏng ở §8.

**③ Ba thư viện trong stack thật đã chuyển sang thu phí.**
MediatR từ v13 · AutoMapper từ v15 · FluentAssertions từ v8. Bản cũ (≤12.x, ≤14.x, v7) còn miễn phí
nhưng **không còn được hỗ trợ**. Đây là chọn 1 trong 3: **pin bản cũ và chấp nhận không support** ·
**chấp nhận ràng buộc RPL-1.5** · **trả tiền**. AutoMapper trước S5 **chưa row nào ghi nhận**.

**④ Kiến thức có hạn dùng.** 4 mục được giữ có ngày hết hạn cứng, gần nhất là **.NET 11 GA
10/11/2026**. Quy tắc: mọi mục loại này phải ship kèm nguồn và ngày "as of".

---

## 10. Hai đính chính đáng nhớ

Ghi lại vì chúng cho thấy **tóm tắt một dòng không phải là bằng chứng** — nguyên tắc đã đúng 3 lần.

**① "Khoảng trống nối dây MCP" ghi ở S4 — không hề tồn tại.**
S4 cứu từ B12 khẳng định rằng công cụ Roslyn MCP cần được chỉ chỗ file solution mới chạy. S5 mở
`SolutionDiscovery.cs` và `Program.cs` ra đọc: **server tự làm, qua 4 bước** — tham số `--solution`
tường minh → quét thư mục làm việc theo chiều rộng 3 cấp → **hỏi MCP host lấy workspace root ở lần
gọi công cụ đầu tiên** → chọn xác định. Chính `.mcp.json` của kit **không truyền tham số nào**, có
chủ đích. ⇒ **S6 không nợ MCP server dòng code nào.**
Chi tiết thật duy nhất còn lại: nó chỉ tìm **xuống dưới, không lên thư mục cha** — mảnh cứu của B12
nói "rồi tới cha" là **sai**.

**② ADR-001 (D17) đã bị chính tác giả kit rút lại.**
Trạng thái của nó ghi *"Superseded by ADR-005"* — tóm tắt S1 không ghi điều đó. Không đổi quyết
định, nhưng đổi lý do: từ *"ta không đồng ý"* thành *"chính tác giả cũng không còn giữ quan điểm
này"*.

*(Đính chính thứ nhất trong chuỗi này là B28 ở S4 — tóm tắt S1 mô tả một phép kiểm tra mà script
không hề thực hiện, và sửa nó đã đổi kết quả từ `skip` thành `combine`.)*

---

## 11. Tra cứu nhanh — toàn bộ 42 mục theo mã

| Mã | Thành phần | Nhóm nguyên nhân |
|---|---|---|
| A03 | `architecture-advisor` | 2 — Q1 |
| A04 | `aspire` | 1 — R4 |
| A07 | `ci-cd` | 1 — R4 |
| A08 | `clean-architecture` | 2 — Q1 *(có vật liệu cứu)* |
| A11 | `container-publish` | 1 — R4 |
| A15 | `docker` | 1 — R4 |
| A16 | `dotnet-init` | 8 — hoãn Q6 |
| A22 | `messaging` | 3 — stack *(có vật liệu cứu)* |
| A27 | `project-setup` | 3 — stack |
| A31 | `scalar` | 3 — stack |
| A35 | `vertical-slice` | 2 — Q1 |
| B02 | `checkpoint` | 6 — sai tầng |
| B04 | `instinct-system` | 5 — xung đột cơ chế |
| B07 | `plan` | 5 — xung đột cơ chế |
| B08 | `spec` | 5 *(có vật liệu cứu)* |
| B11 | `workflow-mastery` | 5 *(có vật liệu cứu)* |
| B12 | `wrap-up` | 5 — đổ theo B04 |
| B13 | `api-designer` | 3 — stack |
| B15 | `code-reviewer` | 4 — đã có A09 |
| B16 | `devops-engineer` | 1 — R4 |
| B17 | `dotnet-architect` | 2 — Q1 |
| B20 | `refactor-cleaner` | 4 — đã có A13 |
| B21 | `security-auditor` | 4 — đã có A32 |
| B22 | `test-engineer` | 4 — phép thử ngữ cảnh |
| B25 | `post-scaffold-restore.sh` | 7 — hook |
| B26 | `post-test-analyze.sh` | 7 — hook |
| B27 | `pre-bash-guard.sh` | 7 — hook, hỏng theo hướng mở |
| B30 | `pre-commit-format.sh` | 7 — hook |
| B33 | `health-check` | 4 *(có vật liệu cứu, bắt buộc S7)* |
| C‑u1 | `mcp-configs/` | 4 — trùng README của server |
| D02 | `rules/architecture.md` | 2 — Q1 |
| D05 | `rules/git-workflow.md` | 6 — sai tầng |
| D15 | `mediatr-to-mediator-migration.md` | 3 — stack |
| D17 | ADR-001 VSA | 2 — Q1 *(đã bị tác giả rút lại)* |
| D20 | ADR-004 HybridCache | 3 — stack *(có vật liệu cứu)* |
| D21 | ADR-005 đa kiến trúc | 2 — Q1 *(tiền đề gốc)* |
| D22 | `decisions/template.md` | 6 — sai tầng |
| D23 | `templates/blazor-app/` | 1 — R4 |
| D24 | `templates/class-library/` | 8 — hoãn Q6 |
| D25 | `templates/modular-monolith/` | 1 — R4 |
| D26 | `templates/web-api/` | 8 — **hoãn, đích còn sống** |
| D27 | `templates/worker-service/` | 8 — **hoãn, đích còn sống** |

---

*Tạo ngày 2026-07-26, sau khi S5 đóng triage. Nguồn: [`TRIAGE.md`](TRIAGE.md) — 94 row, 45 mục nhật
ký quyết định. Tài liệu này không quyết định gì; nó chỉ sắp xếp lại những gì đã quyết định.*
