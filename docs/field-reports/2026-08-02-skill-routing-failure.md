# Vì sao dotnet-standards bị bỏ qua hai lần trong một phiên

> **Field report from a consumer session, filed 2026-08-02.** Written by the
> session that made both mistakes, in a repository this plugin is installed
> into — not by a plugin-maintenance session. Every Superpowers line it quotes
> was re-verified against **6.2.0** before being acted on. The design that
> answers it is `docs/superpowers/specs/2026-08-02-process-handback-design.md`,
> which accepts §§1–7 as evidence and **departs from §8 in two places**: the
> Superpowers-side remedies cannot be executed here (no Superpowers file may be
> modified), and the `UserPromptSubmit` re-fire it proposes cannot catch the
> failure it was aimed at, because the write→review transition is
> model-initiated.

Ngày: 2026-08-02 · Phiên: xây Access Control Core, nhánh `feature/access-control-core`

Báo cáo này truy nguyên hai lần bỏ sót skill trong cùng một phiên làm việc. Cả
hai đều do tôi gây ra. Nhưng cả hai cũng đều có nguyên nhân cấu trúc nằm trong
chính văn bản của các skill, và phần đó đáng sửa vì nó sẽ tái diễn với người dùng
khác.

Mọi trích dẫn dưới đây lấy từ file skill thật, kèm số dòng.

## 1. Hai sự cố

**Sự cố A — pha thiết kế.** Tôi viết spec kiến trúc cho một module MediatR mà
không load `mediatr-messaging`, `module-feature`, `facade-module-architecture`,
`ef-core-data-access` hay `api-surface`. Spec sai về chỗ đặt handler, chỗ đặt
`AddMediatR`, cấu trúc `Services/`, và tự bịa một folder `Contracts/` không tồn
tại trong vốn từ của plugin. Người dùng phát hiện khi review spec.

**Sự cố B — pha review.** Trong hơn 20 lượt review bằng subagent, kể cả final
whole-branch review, tôi không load một skill review nào của dotnet-standards:
`dotnet-review-flow`, `dotnet-code-review`, `dotnet-architecture-review`,
`dotnet-security-review`, `dotnet-performance-review`. Tôi cũng không dùng bốn
subagent reviewer chuyên trách mà plugin cung cấp, mà dùng `general-purpose` cho
tất cả, với khối constraint do tôi tự viết tay từng lượt.

## 2. Nguyên nhân sự cố A — brainstorming cấm load skill khác

`superpowers:brainstorming` nói ba lần, mỗi lần một cách:

```
SKILL.md:13   Do NOT invoke any implementation skill, write any code, scaffold
              any project, or take any implementation action until you have
              presented a design and the user has approved it.

SKILL.md:61   **The terminal state is invoking writing-plans.** Do NOT invoke
              frontend-design, mcp-builder, or any other implementation skill.
              The ONLY skill you invoke after brainstorming is writing-plans.

SKILL.md:132  - Do NOT invoke any other skill. writing-plans is the next step.
```

Dòng 132 không có chữ "implementation". Nó nói thẳng: **không được load skill nào
khác.** Tôi đọc đúng nghĩa đen và tuân theo.

Trong khi đó `choosing-a-dotnet-skill` nói ngược lại, và nói rất cụ thể — nó gọi
đích danh Superpowers:

```
SKILL.md:124  ...while running Superpowers brainstorming, plan writing, or
              subagent-driven development — identify the area the step touches
              and look it up above. **Where a shipped skill owns that area, the
              step must name it.**
```

**Đây là mâu thuẫn trực tiếp giữa hai plugin, về đúng một hành động, trong đúng
một pha.** dotnet-standards đã lường trước va chạm này và viết hướng dẫn cho nó.
Nhưng brainstorming là skill đang cầm quyền điều khiển, và nó cấm.

Một yếu tố cộng hưởng thứ hai, đã sửa: `CLAUDE.md` khi đó viết *"neither MediatR
nor a ConcurrencyHandler exists in this solution"*, mà nhiệm vụ của tôi lại chính
là đưa MediatR vào. Tôi đọc dòng mô tả trạng thái đó thành lời miễn trừ.

## 3. Nguyên nhân sự cố B — subagent-driven-development là một hệ thống đóng

SDD cung cấp trọn bộ máy review và không chừa khe nào để plugin domain gắn vào.

**Nó hard-code loại subagent.** Cả ba template:

```
implementer-prompt.md:6     Subagent (general-purpose):
task-reviewer-prompt.md:11  Subagent (general-purpose):
re-review-prompt.md:11      Subagent (general-purpose):
```

Không dòng nào nói "nếu plugin domain có reviewer agent chuyên trách, dùng cái
đó". Mà dotnet-standards có sáu agent như vậy.

**Nó hard-code file reviewer cho final review.**

```
SKILL.md:74,103,104  "Dispatch final code reviewer
                      (../requesting-code-review/code-reviewer.md)"
SKILL.md:399-400     using superpowers:requesting-code-review's
                     [code-reviewer.md](../requesting-code-review/code-reviewer.md)
```

**Nó bảo tôi tự chép rubric bằng tay** thay vì load rubric có sẵn.
`task-reviewer-prompt.md` có placeholder `[GLOBAL_CONSTRAINTS]`, giải thích là
*"the binding requirements copied verbatim from the plan's Global Constraints
section or the spec"*. Nghĩa là chất lượng review phụ thuộc vào việc tôi có tự
nghĩ ra đúng rủi ro hay không — trong khi rubric tồn tại chính là để không phụ
thuộc vào điều đó.

**Và nó không hề biết plugin domain tồn tại.** Grep `dotnet`, `domain skill`,
`other plugin` trong `subagent-driven-development/SKILL.md` và
`writing-plans/SKILL.md`: **không một kết quả nào.**

Vậy nên câu hỏi "có phải SDD nuốt mất dotnet-standards không" — về cơ bản là
**đúng**. SDD tự đủ đến mức không còn chỗ cho rubric domain. Brainstorming làm
điều tương tự ở pha thiết kế. Cùng một cơ chế: một process skill của Superpowers
tự đóng kín, không chứa câu nào yêu cầu kiểm tra xem có plugin domain nào sở hữu
pha nó đang chạy hay không.

## 4. Thứ lẽ ra đã ngăn được cả hai — và tôi chưa từng mở

`dotnet-feature-flow` tồn tại. Mô tả của nó:

> walking brainstorm, plan, implementation, tests, the four review lenses and the
> git step with human gates in between […] Not for: […] brainstorming, planning
> and TDD themselves — Superpowers.

Đó **chính xác** là quy trình tôi tự lắp bằng tay từ các mảnh Superpowers suốt cả
phiên. Nó nằm trong danh sách skill khả dụng từ đầu. Nó phân định rõ: quy trình
tổng thể là của dotnet-standards, còn brainstorm/plan/TDD thì gọi sang Superpowers
— tức đúng chiều mà lẽ ra phải đi.

Tôi chưa bao giờ mở nó. Vì `using-superpowers` chạy trước và đóng khung toàn bộ
công việc thành "một luồng Superpowers".

## 5. Chênh lệch độ hiện diện của hai plugin

| | Superpowers | dotnet-standards |
|---|---|---|
| Cách vào context | SessionStart hook, **chèn nguyên văn** toàn bộ `using-superpowers` | UserPromptSubmit hook, **một dòng nhắc** |
| Tần suất | Thường trực trong system prompt | *"Emitted once per session"* |
| Ngữ khí | `<EXTREMELY_IMPORTANT>`, "ABSOLUTELY MUST", bảng Red Flags | "load the skill và route từ bảng của nó" |

Superpowers hiện diện liên tục ở mức ưu tiên cao nhất. dotnet-standards được nhắc
đúng một lần, ở prompt đầu tiên, rồi biến mất. Khi tôi chuyển từ pha viết code
sang pha review, không còn gì kéo tôi quay lại bảng router.

## 6. Phần lỗi thuộc về tôi, không đổ cho plugin được

Ba điểm, và không điểm nào có thể quy cho cấu trúc:

1. **Bảng router nằm trong context tôi suốt phiên.** `choosing-a-dotnet-skill:93`
   có hẳn dòng "review" trỏ tới `dotnet-review-flow` cho *"running the subagent
   fleet with the test loop, over a diff or over unchanged code"* — mô tả chính
   xác việc tôi làm. Tôi đọc bảng đó **một lần**, để trả lời câu hỏi "file này đặt
   ở đâu", rồi coi như đã dùng xong. Bảng tra cứu phải được tra lại mỗi khi công
   việc đổi bản chất. Đó là việc của tôi.
2. **Tôi đã tự viết mục "Skill sở hữu từng phần" vào plan** — chứng tỏ tôi hiểu
   nguyên tắc. Nhưng tôi chỉ liệt kê skill cho các vùng *implementation*, vì đó là
   thứ tôi vừa bị bắt lỗi. Tôi sửa triệu chứng, không sửa thói quen.
3. **Tôi đã ghi memory về đúng bài học này ở đầu phiên**, rồi áp dụng nó cho pha
   viết code và không áp cho pha review.

## 7. Cái giá — và giới hạn của điều tôi biết

Tôi sẽ không khẳng định fleet đúng sẽ bắt thêm được gì; chỉ chạy mới biết. Cái
nói được chắc chắn:

- **Lens hiệu năng chưa từng được áp.** Không ai rà N+1, index coverage, cấp phát,
  chi phí đường nóng. Việc `HasJsonbDictionary` serialize ba lần mỗi lần so sánh —
  trên `access_decision`, ghi mỗi lần quét — chỉ lọt vào final review như ghi chú
  phụ, không qua rà soát có hệ thống.
- **Lens kiến trúc và bảo mật là do tôi tự chế**, không phải rubric viết sẵn cho
  repo này.
- `dotnet-review-flow` có kỷ luật *"A finding is verified before it is fixed"* với
  cơ chế xác minh riêng. Tôi có làm điều tương tự, nhưng bằng tay và tự nghĩ ra.

Mặt khác, quy trình chắp vá này **vẫn bắt được** những lỗi thật: leo thang đặc
quyền ở permission, `SubjectExternalReference` mất khi replay, ba biến thể của lỗi
trùng-mã-thành-500, và JWT không mang claim. Nên kết luận không phải "review vô
dụng", mà là "review chạy bằng trí nhớ của tôi thay vì bằng rubric của bạn".

## 8. Đề xuất sửa plugin

**Cho Superpowers — hai câu là đủ:**

- `brainstorming/SKILL.md:132` — đổi *"Do NOT invoke any other skill"* thành
  không cấm **knowledge/domain skill**, chỉ cấm implementation skill. Và nói thêm:
  nếu một domain plugin sở hữu vùng mà một mục thiết kế chạm tới, phải load nó
  trước khi viết mục đó. Hiện tại dòng này chặn đúng thứ cần nhất.
- `subagent-driven-development/SKILL.md` — thêm một bước trong Setup: *kiểm tra
  xem session có plugin domain nào cung cấp rubric review hoặc reviewer agent
  chuyên trách không; nếu có, dùng chúng thay cho template mặc định.* Và trong ba
  template, đổi `Subagent (general-purpose)` thành placeholder `[AGENT_TYPE]`.

**Cho dotnet-standards:**

- `choosing-a-dotnet-skill` đã làm đúng phần của nó (dòng 124). Nhưng nó chỉ được
  đọc một lần. Cân nhắc đổi hook `UserPromptSubmit` từ *once per session* sang bắn
  lại khi công việc đổi pha — ít nhất là khi prompt có "review".
- `dotnet-feature-flow` là câu trả lời đúng cho cả phiên này mà không ai tìm tới.
  Mô tả của nó nên nói thẳng rằng nó **thay thế** việc tự lắp Superpowers
  brainstorming + writing-plans + subagent-driven-development bằng tay, chứ không
  chỉ liệt kê các pha.

## 9. Việc nên làm với nhánh hiện tại

Chạy `dotnet-standards:dotnet-review` trên nhánh này trước khi xử lý năm finding
của final review. Kết quả sẽ trả lời câu hỏi mục 7 bằng dữ liệu: fleet đúng có bắt
thêm gì không, đặc biệt ở lens hiệu năng vốn chưa ai chạm. Nếu không bắt thêm gì,
đó cũng là thông tin có giá trị cho plugin.
