# Feedback cho plugin dotnet-standards — từ một lần dùng thật trên repo BE-Ops-Service

  ## Ngữ cảnh

  Repo: API .NET 8 (ASP.NET Core + EF Core + PostgreSQL), tầng test dùng xUnit v3 3.2.2,
  Testcontainers, Respawn, Shouldly, NSubstitute — đúng bộ mà skill `dotnet-testing` quy định.
  Chú thích code trong repo viết bằng tiếng Việt.

  Nhiệm vụ đã thực hiện: gộp 4 fixture của tầng integration test về 1, bỏ Respawn, chuyển sang
  "mỗi test tự mang dữ liệu duy nhất". Kết quả: từ chỗ số ca hỏng dao động 13/59/87 trên cùng
  một commit, tầng này về 146/146 xanh, ổn định qua nhiều lần chạy, thời gian 11–16 giây.

  Trong quá trình đó lộ ra 6 điểm mà plugin nên sửa. Mục 5 là mục quan trọng nhất và cũng là
  mục ít hiển nhiên nhất.

  ---

  ## Mục 1 — `references/integration-testing.md` thiếu luật "một factory cho mỗi assembly"

  **Vấn đề.** Tài liệu mô tả `ApiFixture` như thể nó là fixture duy nhất, nhưng không nói ở đâu
  rằng đó là *ràng buộc*. Người viết tính năng thứ hai cần cấu hình host khác (environment khác,
  scheme auth khác) sẽ tạo fixture thứ hai bên cạnh — hoàn toàn hợp lệ theo tài liệu hiện tại.

  **Bằng chứng.** Repo này có hai lớp cùng kế thừa `WebApplicationFactory<Program>`. Cả hai đều
  phải ghi đè connection string, mà đường ghi đè đáng tin cậy duy nhất là
  `Environment.SetEnvironmentVariable` — chính tài liệu ngầm thừa nhận điều này khi nói
  `AddEnvironmentVariables()` là nguồn cấu hình chạy sau cùng. Biến môi trường thuộc về
  *process*, không thuộc về fixture. xUnit chạy các collection song song, nên hai fixture ghi
  đè connection string của nhau, và một fixture migrate nhầm container của fixture kia. Triệu
  chứng quan sát được: `42P01: relation "permission" does not exist` ném ra từ
  `InitializeAsync()`, và số ca ĐẠT dao động giữa các lần chạy dù không đổi dòng code nào.

  **Instruction.** Thêm vào `references/integration-testing.md`, ngay sau phần
  "The integration fixture", một tiểu mục *One factory per assembly*: một test assembly được
  phép có **đúng một** lớp kế thừa `WebApplicationFactory<TEntryPoint>`. Giải thích bằng cơ chế
  chứ đừng chỉ ra lệnh: config override đáng tin cậy đi qua biến môi trường, biến môi trường
  thuộc về process, nên fixture thứ hai không phải là "thêm một fixture" mà là "một cuộc đua".
  Nêu cách xử lý khi hai nhóm test cần cấu hình host khác nhau: tham số hoá một factory, đừng
  nhân bản nó. Và cảnh báo rõ rằng `[assembly: CollectionBehavior(DisableTestParallelization = true)]`
  **không** sửa được — repo này đã thử, kết quả tệ hơn: dựng `WebApplicationFactory<Program>`
  lần thứ hai một cách tuần tự trong cùng process đụng `HostFactoryResolver` và sinh
  `InvalidOperationException: The entry point exited without ever building an IHost`.

  ---

  ## Mục 2 — Skill dạy `ICollectionFixture` là cấp cao nhất, nhưng `IAssemblyFixture` mới là cấp đúng

  **Vấn đề.** Tài liệu viết dứt khoát *"`ICollectionFixture<T>`, not `IClassFixture<T>`"* và giải
  thích rằng class fixture khởi động một container mỗi test class. Lập luận đó dừng sớm đúng một
  bậc: collection fixture khởi động một container **mỗi collection**.

  **Bằng chứng.** Repo này có 4 collection, do đó có 4 container Postgres. Người tạo ra trạng thái
  đó đã làm **đúng theo tài liệu**. Trong khi `xunit.v3` 3.2.2 có sẵn `AssemblyFixtureAttribute`
  và `IAssemblyFixture` — đã kiểm chứng trực tiếp bằng cách quét
  `xunit.v3.extensibility.core/3.2.2/lib/netstandard2.0/xunit.v3.core.dll`, không phải nhớ — và
  đó mới là thứ hiện thực đúng câu tiêu đề của chính mục đó: *"Start the containers once for the
  whole suite"*.

  **Instruction.** Sửa phần *Sharing containers and resetting state* thành ba bậc thay vì hai:
  `IClassFixture` (một container mỗi class), `ICollectionFixture` (một container mỗi collection —
  vẫn nhân bản nếu assembly có nhiều collection, và đây chính là cách bốn container ra đời),
  `[assembly: AssemblyFixture(typeof(T))]` (một container cho cả assembly). Ghi rõ
  `IAssemblyFixture` là API của xUnit v3 và không có ở v2, nên mọi mẫu code chép từ v2 sẽ không
  có nó. Ghi rõ hệ quả đi kèm: bỏ `[Collection]` là bỏ luôn tuần tự hoá, nên chỉ làm được khi
  các test không giẫm dữ liệu lên nhau.

  ---

  ## Mục 3 — Respawn đang được trình bày như lựa chọn duy nhất, trong khi có ba

  **Vấn đề.** Skill viết *"Reset beats re-create"* và chỉ so Respawn với drop-and-migrate. Lựa
  chọn thứ ba không được nhắc: **không dọn gì cả, mỗi test mang dữ liệu bất giao**.

  **Bằng chứng.** Nó không chỉ nhanh hơn — nó là thứ duy nhất trong ba cái cho phép chạy song
  song, vì hai cái kia đều buộc tuần tự hoá (chính skill giải thích lý do: *"parallel tests
  resetting it would delete each other's rows"*). Trong repo này, **20 trên 25 test class đã
  viết theo kiểu bất giao sẵn**, và nhóm 11 class chạy dưới fixture không có reset đã sống tốt
  từ trước. Respawn đang trả cái giá tuần tự hoá cho một thứ mà 80% test không cần. Sau khi bỏ,
  cả tầng chạy trong 11–16 giây.

  **Instruction.** Đổi mục đó từ một quy định thành một bảng chọn ba hướng — Respawn, TRUNCATE
  thủ công, dữ liệu bất giao — kèm tiêu chí chọn, thay vì chỉ kể ưu điểm của Respawn. Nêu cái
  giá ẩn của Respawn (buộc tuần tự hoá cả collection). Nêu điều kiện để hướng bất giao dùng
  được, và nêu như một hợp đồng phải giữ khi viết test mới:

  - mọi giá trị chạm unique index phải sinh từ `Guid.NewGuid()`, không viết cứng;
  - mọi `CountAsync` / `ToListAsync` / `SingleAsync` / `Assert.Empty` phải có predicate thu về
    dữ liệu của chính test đó.

  Và cảnh báo về hai loại test **không** chuyển sang bất giao được chỉ bằng cách sinh Guid — cả
  hai đều có thật trong repo này:

  - Test khẳng định trần phân trang trên toàn bảng. Phải thêm bộ lọc theo tiền tố riêng của test
    vào tầng query, không phải đổi dữ liệu.
  - Test ghi vào một **tenant cố định đọc từ cấu hình** thay vì tenant sinh mới. Guid tách được
    dữ liệu con, nhưng không tách được hàng dùng chung mà mọi lời gọi đều phải bảo đảm tồn tại.
    Cách xử lý đã dùng và nên đưa vào skill: gom riêng nhóm class đó vào **một `[CollectionDefinition]`
    không mang fixture nào**, thuần tuý để tuần tự hoá chúng với nhau, phần còn lại của assembly
    vẫn song song. Đây là mẫu hữu ích và không hiển nhiên.

  ---

  ## Mục 4 — Cảnh báo về PowerShell làm hỏng file source

  **Vấn đề.** Không có tài liệu nào trong plugin cảnh báo điều này, và nó gây ra một chẩn đoán
  sai mất thời gian.

  **Bằng chứng.** Dùng `Get-Content -Raw` + `Set-Content -Encoding utf8` để thêm một dòng
  attribute vào 6 file test. PowerShell 5.1 khi không được chỉ định `-Encoding` sẽ đọc file
  không BOM theo codepage ANSI (Windows-1252), rồi ghi lại thành UTF-8 — mã hoá hai lần toàn bộ
  ký tự non-ASCII. Hậu quả: một test dùng ký tự Kelvin Sign (U+212A) làm dữ liệu chuyển sang đỏ
  **tất định 4/4 lần**, trông y hệt một hồi quy thật. Rủi ro này cao hơn hẳn ở repo có chú thích
  không phải tiếng Anh — repo này chú thích bằng tiếng Việt nên gần như file nào cũng dính.

  **Instruction.** Thêm vào tài liệu quy ước chung của plugin (phần nói về công cụ và môi trường
  Windows) một cảnh báo: không sửa file source bằng `Get-Content`/`Set-Content`; dùng tool
  sửa file của harness. Nếu buộc phải thao tác hàng loạt bằng shell thì đọc/ghi qua
  `[System.IO.File]::ReadAllBytes` / `WriteAllBytes` với encoding khai báo tường minh. Kèm cách
  nhận ra khi đã lỡ (`git diff --stat` phình lớn hơn nhiều so với thứ định sửa) và phép đảo
  ngược lossless: `ReadAllBytes` → bỏ BOM → `UTF8.GetString` → `GetEncoding(1252).GetBytes` →
  `WriteAllBytes`.

  ---

  ## Mục 5 — QUAN TRỌNG NHẤT: skill chưa nói rằng suite dao động **che giấu lỗi thật**, không chỉ gây phiền

  **Vấn đề.** Toàn bộ tài liệu của plugin coi test không ổn định là vấn đề về *độ tin cậy của
  quy trình* — gây khó chịu, làm chậm, khó biết khi nào xanh. Không chỗ nào nói ra hệ quả nặng
  hơn nhiều: **một suite dao động biến mọi ca đỏ thành nhiễu đã biết, và lỗi thật nấp trong đó.**

  **Bằng chứng — đây là phần đáng đọc kỹ.** Trước khi sửa, tầng integration có 13 ca đỏ và tổng
  số dao động 13/59/87 trên cùng một commit. Vì không ai phân biệt được ca nào đỏ do code và ca
  nào đỏ do fixture đua nhau, cả 13 ca bị gộp chung vào "nhiễu đã biết" và ghi trong báo cáo là
  nằm ngoài phạm vi. Sau khi gộp fixture, tầng này ổn định và 13 ca tách ra rõ ràng:

  - **8 ca là artifact của fixture** — tự xanh, không sửa dòng code nghiệp vụ nào.
  - **5 ca là tín hiệu thật.** Trong đó 3 ca là test lạc hậu vô hại. Còn **2 ca là lỗi production
    nghiêm trọng đã nằm im nhiều ngày**:

    1. Migration `20260730040542_UpdateDeviceRefreshTokenUniqueConstraint` được commit ở
       `0490110` (*"feat(device-management): enforce single device session"*) rồi **bị xoá** ở
       `1e76f75` (*"refactor(devices): align module boundaries and deletion guards"*). Nó là thứ
       đặt `unique: true` lên `IX_device_refresh_token_DeviceId`. Hậu quả: ràng buộc một-phiên-mỗi-thiết-bị
       biến mất khỏi cả model lẫn database; và database nào đã chạy migration đó trước khi nó bị
       xoá thì vẫn còn index, database dựng mới thì không — hai môi trường lệch schema mà không
       có gì báo.
    2. Migration `20260804130322_updateEntity`, đi kèm commit tiêu đề
       *"refactor(device-types): align settings"*, đổi cột `booth.Name` từ `citext` sang `text`.
       Service kiểm trùng tên vẫn còn nguyên đoạn chú thích giải thích rằng nó **cố tình** không
       bọc `ToLower()` *vì đã dựa vào `citext`*. Cơ sở bị rút, chú thích ở lại. Tính duy nhất tên
       khu vực trong một sự kiện hiện không còn bỏ qua hoa thường ở production.

  Cả hai lỗi đều có test bắt đúng, đỏ đều đặn, và bị bỏ qua nhiều ngày **chỉ vì chúng lẫn trong
  một tầng test mà không ai còn tin nữa**.

  **Instruction.** Thêm vào skill `dotnet-testing` một nguyên tắc mới ở phần Core Principles,
  đại ý: *một tầng test không tất định phải được coi là hỏng ở mức cao hơn một tầng test đỏ, và
  việc ổn định hoá nó là điều kiện tiên quyết để bất kỳ ca đỏ nào được phép mang ý nghĩa.* Nêu
  cơ chế: khi tổng số dao động, chi phí phân loại từng ca đỏ vượt quá lợi ích, nên đội sẽ hợp lý
  hoá cả cụm thành "nhiễu đã biết" — và đó chính là chỗ lỗi thật trú ẩn. Kèm hai hệ quả thao tác:

  - Cấm dùng tổng toàn suite làm bằng chứng khi tầng chưa tất định; chỉ filter riêng mới đáng tin.
  - Khi ổn định hoá xong, **phải phân loại lại toàn bộ ca đỏ tồn đọng**, từng ca một, thay vì
    mang danh sách cũ sang. Đây là bước mà quy trình hiện tại không có, và là bước tìm ra hai
    lỗi trên.

  Đồng thời bổ sung tiêu chí xác minh: sửa một vấn đề dao động thì bằng chứng phải là **nhiều
  lần chạy liên tiếp trên cùng một commit cho cùng một kết quả**, không phải một lần xanh. Ở đây
  đã dùng 3–4 lần cho mỗi trạng thái, và chính điều đó phát hiện được một flake còn sót (1/3 lần)
  mà một lần chạy sẽ bỏ lọt.

  ---

  ## Mục 6 — Rubric review nên bắt được "một migration đã committed bị xoá"

  **Vấn đề.** Luật cứng "không bao giờ xoá một migration đã committed" nằm trong CLAUDE.md của
  repo, nhưng không có agent review nào của plugin kiểm được nó, và vi phạm này đã lọt qua.

  **Bằng chứng.** Vi phạm phát hiện được bằng một lệnh git đơn giản:
  `git log --all --diff-filter=D --name-only -- "*Migrations/*"` cộng với
  `git merge-base --is-ancestor <commit-thêm> HEAD` để xác nhận nó từng nằm trong lịch sử nhánh
  hiện tại. Đây là kiểm tra cơ học, rẻ, và hậu quả khi bỏ sót thì nặng (lệch schema giữa các môi
  trường, không có cảnh báo).

  **Instruction.** Thêm vào rubric của `dotnet-architecture-reviewer` (hoặc rubric sở hữu
  `ef-core-data-access`) một hạng mục kiểm: trong khoảng diff đang review, có file nào dưới
  `Migrations/` bị xoá hoặc đổi tên không; nếu có, xác minh bằng `git merge-base --is-ancestor`
  xem nó đã từng committed chưa, và báo là finding mức cao kèm nội dung migration đã mất. Nêu
  luôn cách sửa đúng để reviewer khuyến nghị được: **không** phục hồi file cũ (sẽ vỡ ở database
  đã có dòng tương ứng trong `__EFMigrationsHistory`), mà khai lại trong entity configuration rồi
  sinh một migration mới chịu được cả hai trạng thái.

  ---

  ## Điểm cuối, về chính plugin

  Sáu mục trên độc lập nhau và có thể sửa riêng lẻ. Mục 1, 2, 3 là ba mặt của cùng một thiếu sót:
  skill mô tả **một** cấu hình tầng integration test đúng, nhưng không mô tả **không gian lựa chọn**
  và tiêu chí chọn — nên khi repo lớn lên và cần cấu hình thứ hai, người ta nhân bản cái đang có,
  và điều đó không sai theo bất kỳ câu nào trong tài liệu. Mục 5 là thứ nên sửa trước nếu chỉ sửa
  được một mục.