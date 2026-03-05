# Contact management system requirements
_Exported on 05/03/2026 at 15:59:13 EET from Cursor (2.6.11)_

---

**User**

Functional Requirements
Core Entity: Contact
Build a system to manage contacts with the following characteristics:
Standard contact information (e.g. name, email, phone, timestamps, etc.)
Contact custom fields: users can define additional fields with different data types (string, integer, bool) without schema changes
API Endpoints
Implement a RESTful API that supports the following operations:
Contacts:
Create a contact
Retrieve contacts with support for pagination, sorting (default: by creation date), filtering
Retrieve a single contact by identifier
Update a contact
Delete a contact
Bulk Merge — an atomic operation that accepts a collection of contacts and:
Matches existing contacts by email
Updates matched contacts with new data
Creates new contacts for non-matched entries
Removes contacts that exist in DB but are absent from the input collection
Custom Fields:
List all custom fields
Create custom field
Delete custom field
Assign custom field value to a contact
Technical Requirements
Area
Requirement
Framework
ASP.NET Core Web API (.NET 8+)
Database
SQL Server
Data Access
Entity Framework Core or Dapper (your choice)
Testing
Integration tests covering all endpoints
Database Design
Optimized for read-heavy workloads with appropriate indexing strategy

Evaluation Criteria
API design, database schema, code quality, performance awareness, test coverage, AI tool usage (critical evaluation, not copy-paste)
Deliverables

Сформулюй коротко ключові припущення, ризики і запропонуй мінімальний план реалізації на годину

---

**Cursor**


Переглядаю структуру проєкту, щоб зрозуміти поточний стан і підготувати план.



Маю повну картину: проєкт — шаблон ASP.NET Core 8 (minimal API), без БД та сутностей. Формую план із припущеннями, ризиками та мінімальним планом на 1 годину.


---

**User**

База даних і схема, спроектуй таблиці та мінімально додай потрібні  EF Core + SqlServer codefirst

---

**Cursor**


Додаю EF Core і SQL Server, створюю сутності та DbContext і налаштовую Code First.


---

**User**

зроби конфігурацію окремими класами Configuration

---

**Cursor**


Створюю окремі класи конфігурації для кожної сутності та оновлюю DbContext.



Зміни внесені: конфігурація винесена в окремі класи.

**Що зроблено:**

1. **`Data/Configurations/ContactConfiguration.cs`** — `IEntityTypeConfiguration<Contact>`: ключ, обмеження на Name/Email/Phone, індекси по Email та CreatedAt.

2. **`Data/Configurations/CustomFieldDefinitionConfiguration.cs`** — `IEntityTypeConfiguration<CustomFieldDefinition>`: ключ, обмеження на Name, унікальний індекс по Name.

3. **`Data/Configurations/ContactCustomFieldValueConfiguration.cs`** — `IEntityTypeConfiguration<ContactCustomFieldValue>`: композитний ключ, зв’язки з Contact і CustomFieldDefinition (Cascade), індекс по (ContactId, CustomFieldDefinitionId).

4. **`Data/AppDbContext.cs`** — у `OnModelCreating` залишено лише виклик:
   ```csharp
   modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
   ```
   Усі конфігурації підхоплюються з поточної збірки автоматично.

---

**User**

2. Контакти — API (≈20 хв)





Create: POST /api/contacts — тіло з name, email, phone; повертати 201 + Id.



Get by id: GET /api/contacts/{id} — 404 якщо немає.



List: GET /api/contacts?page=1&pageSize=20&sortBy=CreatedAt&sortOrder=desc&email=... — пагінація, сортування за полями (default: CreatedAt desc), фільтр по email (мінімальний набір).



Update: PUT /api/contacts/{id} — оновлення полів.



Delete: DELETE /api/contacts/{id}.

Реєстрація сервісів (DbContext, опціонально репозиторій) у Program.cs; endpoints через Map* з групою /api/contacts.

переходимо до API реалізуй мінімальний набір ендпоінтів у .NET 8, та контроллерів, контроллери не мають містити ніякої бизнес логіки 

---

**Cursor**


Реалізую API контактів через контролери без бізнес-логіки та сервісний шар для логіки.



Перевіряю, чи EF Core коректно переводить сортування через змінну типу Func:



Роблю сортування через явний switch, щоб EF коректно генерував SQL:



Підсумок змін:

**1. Моделі (DTO)** — `Models/`
- `CreateContactRequest` — Name, Email?, Phone
- `UpdateContactRequest` — те саме для PUT
- `ContactResponse` — Id, Name, Email, Phone, CreatedAt, UpdatedAt
- `ContactListQuery` — Page, PageSize, SortBy (default CreatedAt), SortOrder (default desc), Email (фільтр)
- `PagedResult<T>` — Items, TotalCount, Page, PageSize

**2. Сервіс** — `Services/`
- `IContactService` — GetByIdAsync, GetListAsync, CreateAsync, UpdateAsync, DeleteAsync
- `ContactService` — реалізація з використанням `AppDbContext`: пагінація, сортування за Name/Email/Phone/CreatedAt/UpdatedAt, фільтр по email (contains). Вся бізнес-логіка лише тут.

**3. Контролер** — `Controllers/ContactsController.cs`
- Маршрут: `api/contacts`
- **GET** `api/contacts/{id}` → 200 + тіло або 404
- **GET** `api/contacts?page=1&pageSize=20&sortBy=CreatedAt&sortOrder=desc&email=...` → 200 + `PagedResult<ContactResponse>`
- **POST** `api/contacts` → 201 Created + `Location: .../api/contacts/{id}` + тіло
- **PUT** `api/contacts/{id}` → 200 або 404
- **DELETE** `api/contacts/{id}` → 204 No Content або 404  

Контролер лише викликає сервіс і повертає результат, без логіки.

**4. Program.cs**
- `AddControllers()`, `MapControllers()`
- `AddScoped<IContactService, ContactService>()`

Збірка проєкту має проходити успішно; перевірка через Swagger: `/swagger` після запуску API.

---

**User**

Bulk Merge (≈10 хв)





POST /api/contacts/bulk-merge: тіло — масив контактів (наприклад, id опціонально, name, email, phone).



Логіка в одному сервісі/хендлері в межах однієї транзакції:





по email знайти існуючі контакти;



оновити знайдені;



додати нові (немає в БД по email);



видалити контакти, яких немає в переданому масиві (їхні email не в списку вхідних).



Повернути підсумок (created/updated/deleted counts або список Id).

пееходимо до Bulk Merge логіка має бути в одному сервісі 
виконуватись в межах однієї транзакції
перевіряти дублікати email 
повернути результат

---

**Cursor**


Додаю Bulk Merge: DTO, метод у сервісі (одна транзакція, перевірка дублікатів email), ендпоінт у контролері.



Оптимізую: один SaveChanges після циклу замість виклику після кожної операції.



Ось що зроблено:

**1. Моделі**
- `BulkMergeContactItem` — Name, Email?, Phone (id не використовується, зіставлення по email).
- `BulkMergeRequest` — масив `Items` (контактів).
- `BulkMergeResult` — CreatedCount, UpdatedCount, DeletedCount, CreatedIds, UpdatedIds, DeletedIds.

**2. Сервіс `ContactService.BulkMergeAsync` (одна транзакція)**
- **Перевірка дублікатів email** у вхідному масиві — якщо є повторення, викидається `ArgumentException` з переліком дублікатів.
- **Транзакція** — `await _db.Database.BeginTransactionAsync`; при помилці — `RollbackAsync`.
- Зіставлення по **email** (без урахування регістру): завантажуються існуючі контакти з БД за множиною email з запиту.
- **Оновлення** — для кожного вхідного email, що є в БД, оновлюються Name, Email, Phone, UpdatedAt.
- **Створення** — запис без email або з email, якого немає в БД, додається як новий контакт.
- **Видалення** — контакти в БД, чий email не входить у список email з запиту, видаляються (`ExecuteDeleteAsync`).
- Повертається **BulkMergeResult** з кількостями та списками Id (created/updated/deleted).

**3. Контролер**
- **POST** `api/contacts/bulk-merge` — тіло `BulkMergeRequest`; при дублікатах email у тілі — **400 Bad Request** з повідомленням у полі `error`.

Маршрут `[HttpPost("bulk-merge")]` забезпечує, що запит до `api/contacts/bulk-merge` обробляється цим ендпоінтом, а не як `POST api/contacts` з `id = "bulk-merge"`.

---

**User**

Custom Fields — API (≈10 хв)





List: GET /api/custom-fields — усі CustomFieldDefinitions.



Create: POST /api/custom-fields — Name + DataType.



Delete: DELETE /api/custom-fields/{id} (з каскадом або видаленням значень).



Assign: PUT/PATCH /api/contacts/{id}/custom-fields/{fieldId} — тіло з value (string/int/bool відповідно до DataType). Зберегти в ContactCustomFieldValues.

---

**Cursor**

Зараз додам мінімальний Custom Fields API:

- **Custom field definitions**:
  - `GET /api/custom-fields` — список усіх `CustomFieldDefinition` (DTO).
  - `POST /api/custom-fields` — створення (Name + DataType, з валідацією унікальності Name).
  - `DELETE /api/custom-fields/{id}` — видалення; завдяки каскадам з конфігурації видаляються й значення.

- **Assign до контакту**:
  - DTO для асайну: одне з полів `stringValue`/`intValue`/`boolValue` (перевірка, що **рівно одне** задане, та що тип відповідає `DataType` поля).
  - Сервіс `ICustomFieldService` / `CustomFieldService`:
    - шукає контакт і поле; якщо немає — кидає `KeyNotFoundException`;
    - шукає/створює `ContactCustomFieldValue`, записує значення в потрібну колонку, інші колонки чистить, `SaveChangesAsync`.
  - В `ContactsController` додається `PUT /api/contacts/{id}/custom-fields/{fieldId}`:
    - викликає сервіс;
    - `KeyNotFoundException` → 404, `ArgumentException` → 400, успіх → 204 No Content.

Також зареєструю `ICustomFieldService` у `Program.cs`.

---

**User**

У ContactManagement.Tests.Integration: WebApplicationFactory з заміною БД на InMemory покрий критичні сценарії

---

**Cursor**


Перевіряю наявність типів BulkMerge і додаю їх при потребі. Додаю інтеграційні тести.



Відновлюю видалені DTO для bulk-merge і додаю інтеграційні тести.



Перевіряю реєстрацію DbContext у ASP.NET Core:



Видаляю порожній UnitTest1.cs і перевіряю лінтер.



Ось підсумок змін.

**1. API (відновлені DTO для bulk-merge)**  
- `Models/BulkMergeContactItem.cs`, `Models/BulkMergeRequest.cs`, `Models/BulkMergeResult.cs` — щоб проєкт збирався після посилань у контролері та сервісі.

**2. Видимість Program для тестів**  
- `ContactManagement.Api/Program.Visibility.cs` — оголошено `public partial class Program`, щоб `WebApplicationFactory<Program>` міг використовувати точку входу.

**3. Тестовий проєкт**  
- Пакети: `Microsoft.AspNetCore.Mvc.Testing` (8.0.11), `Microsoft.EntityFrameworkCore.InMemory` (8.0.11).
- **CustomWebApplicationFactory** (`CustomWebApplicationFactory.cs`):
  - Наслідує `WebApplicationFactory<ContactManagement.Api.Program>`.
  - У `ConfigureWebHost` викликає `ConfigureTestServices`: видаляє реєстрації `DbContextOptions<AppDbContext>` та `AppDbContext`, реєструє `AppDbContext` з `UseInMemoryDatabase("IntegrationTests")`, викликає `EnsureCreated()` для створення схеми.

**4. Інтеграційні тести** (`ContactsApiTests.cs`, один `HttpClient` через `IClassFixture<CustomWebApplicationFactory>`):

- **CreateContact_Returns201_AndGetById_ReturnsContact** — POST `/api/contacts`, перевірка 201, тіло з Id/Name/Email/Phone, потім GET по Id і перевірка даних.
- **GetById_WhenNotFound_Returns404** — GET `/api/contacts/99999` → 404.
- **GetList_ReturnsPagedResult_WithDefaultSort** — GET `/api/contacts?page=1&pageSize=5` → 200, перевірка структури `PagedResult` (Items, Page, PageSize, TotalCount).
- **UpdateContact_Returns200_AndGetById_ReturnsUpdated** — створення контакту, PUT по Id з новими даними, перевірка 200 і даних при GET.
- **DeleteContact_Returns204_AndGetById_Returns404** — створення контакту, DELETE по Id → 204, потім GET → 404.
- **BulkMerge_CreatesUpdatesAndDeletes_InOneCall** — створюється контакт з `existing@merge.com`, викликається POST `/api/contacts/bulk-merge` з двома елементами (новий `new@merge.com` і оновлення `existing@merge.com`), перевірка CreatedCount/UpdatedCount і даних отриманих контактів по Id з результату.
- **BulkMerge_WithDuplicateEmails_Returns400** — bulk-merge з двома елементами з однаковим email → 400.

Запуск тестів (потрібен мережевий доступ для `dotnet restore`):

```bash
cd ContactManagement.Tests.Integration
dotnet restore
dotnet test
```

---

**User**

у стилі AAA з чіткими коментарями, перевір чи покриває базовий функціонал

---

**Cursor**


Переписую тести в стилі AAA з чіткими коментарями і додаю перевірки для повного покриття базового функціоналу (включно з Update/Delete для неіснуючого id).



Ось що зроблено.

**Стиль AAA**  
Кожен тест має три блоки з коментарями:
- **// Arrange** — підготовка даних і запитів
- **// Act** — один виклик API
- **// Assert** — перевірка статусу та даних

**Покриття базового функціоналу**

| Сценарій | Тест | Що перевіряє |
|----------|------|----------------|
| **Create** | `Create_ValidRequest_Returns201AndContactWithId` | POST → 201, тіло з Id, Name, Email, Phone |
| | `Create_ThenGetById_ReturnsSameContact` | після створення GET по Id повертає той самий контакт |
| **GetById** | `GetById_ExistingId_Returns200AndContact` | GET існуючого Id → 200 і коректний контакт |
| | `GetById_NonExistentId_Returns404` | GET неіснуючого Id → 404 |
| **GetList** | `GetList_WithPagination_Returns200AndPagedResult` | GET з page/pageSize → 200, структура PagedResult, Page, PageSize, TotalCount, Items |
| | `GetList_WithEmailFilter_ReturnsOnlyMatchingContacts` | GET з email → лише контакти з цим email (фільтр) |
| **Update** | `Update_ExistingContact_Returns200AndUpdatedData` | PUT існуючого → 200 і оновлені дані |
| | `Update_NonExistentId_Returns404` | PUT неіснуючого Id → 404 |
| **Delete** | `Delete_ExistingContact_Returns204` | DELETE існуючого → 204 |
| | `Delete_ExistingContact_ThenGetById_Returns404` | після DELETE контакт більше не повертається (GET → 404) |
| | `Delete_NonExistentId_Returns404` | DELETE неіснуючого Id → 404 |
| **BulkMerge** | `BulkMerge_NewAndExistingEmails_CreatesNewAndUpdatesExisting` | create + update за email, перевірка результатів по Id |
| | `BulkMerge_DuplicateEmailsInRequest_Returns400` | дублікат email у тілі → 400 |

У клас додано коментар-огляд: які саме операції покриваються (Create, GetById, GetList з пагінацією та фільтром, Update, Delete, BulkMerge). Додано тести для Update/Delete по неіснуючому Id та для фільтра GetList по email, щоб базовий функціонал був покритий повністю.

---

**User**

перевір Program.cs та прибери зайве

---

**Cursor**


Видаляю шаблонний код погоди — ендпоінт і тип `WeatherForecast`.



Зміни внесені. Підсумок:

**Видалено:**
- Ендпоінт **GET /weatherforecast** (шаблонний minimal API)
- Масив `summaries` і логіка погоди
- Тип **WeatherForecast**
- Коментар `// Configure the HTTP request pipeline.`

**Залишено в Program.cs лише потрібне:**
- реєстрація сервісів (Swagger, Controllers, DbContext, IContactService);
- pipeline: Swagger у Development, HTTPS redirection, MapControllers;
- `app.Run()`.

Поточний вміст файлу:



Видаляю `AddEndpointsApiExplorer()` — він потрібен лише для minimal API; у проєкті лише контролери.



Готово. `AddEndpointsApiExplorer()` прибрав — він потрібен тільки для minimal API, у тебе всі ендпоінти йдуть з контролерів, Swagger їх підхоплює через `AddSwaggerGen()`. Якщо після зміни щось перестане з’являтися в Swagger UI, можна буде повернути цей виклик.

---

**User**

500 в ContactsApiTests.cs:line 257 перейди на SQLite in‑memory

---

**Cursor**


Переходимо на SQLite in-memory: додаємо пакет і оновлюємо фабрику — зберігаємо з’єднання відкритим, щоб in-memory база не зникала.



Зміни внесені. Підсумок:

**1. `ContactManagement.Tests.Integration.csproj`**
- Замінено пакет `Microsoft.EntityFrameworkCore.InMemory` на `Microsoft.EntityFrameworkCore.Sqlite` (версія 8.0.11).

**2. `CustomWebApplicationFactory.cs`**
- Підключено **SQLite in-memory**: створюється `SqliteConnection("DataSource=:memory:")`, викликається `Open()` і цей connection передається в `UseSqlite(_sqliteConnection)`.
- З’єднання зберігається в полі `_sqliteConnection`, щоб in-memory база жила протягом усіх тестів (у SQLite in-memory база існує, поки відкритий хоча б один connection).
- У `Dispose(bool)` додано `_sqliteConnection?.Dispose()`, щоб connection коректно закривався разом із фабрикою.
- Прибрані непотрібні `using` (у фабриці не використовуються `System.Net`, `Http.Json`, `Api.Models`, `TestHost`).

Чому це має прибрати 500: провайдер **InMemory** не підтримує частину операцій (зокрема `ExecuteDeleteAsync` і повноцінні транзакції), через що bulk-merge міг падати з помилкою на сервері. **SQLite in-memory** поводиться як звичайна реляційна БД, тому транзакції та bulk-операції мають виконуватися коректно. Запусти тести ще раз; якщо 500 з’явиться знову, надішли текст помилки з тіла відповіді або логів.

---

**User**

додай інтеграційні тести для custom fields

---

**Cursor**


Перевіряю наявність API для custom fields і додаю реалізацію та інтеграційні тести.



Реалізую API custom fields і додаю інтеграційні тести.


---

**User**

Ендпоїнти для custom fields Потрібні контролер, сервіс, DTO, валідація та логіка присвоєння значень.

---

**Cursor**


Реалізую сервіс, контролер, валідацію та ендпоінт присвоєння.



Ось що зроблено:

**1. DTO**
- **CustomFieldDefinitionResponse** — вже був (Id, Name, DataType).
- **CreateCustomFieldRequest** — Name з атрибутами `[Required]`, `[MinLength(1)]`, `[MaxLength(200)]`, DataType.
- **AssignCustomFieldValueRequest** — ValueString?, ValueInt?, ValueBool? (одне має бути задане згідно з DataType поля).

**2. Сервіс `CustomFieldService`**
- **GetListAsync** — усі визначення полів, відсортовані за Name.
- **GetByIdA

---

**User**

додай unique index на Email, обробляти дублі в сервісі

---

**Cursor**


Додаю унікальний індекс на Email і перевірку дублікатів у сервісі.



Додаю обробку ArgumentException у контролері для Create та Update, щоб повертати 400.



Додаємо міграцію для унікального індексу:



Ось зміни:

**1. Унікальний індекс на Email**

- **ContactConfiguration.cs** — індекс по `Email` зроблено унікальним: `HasIndex(x => x.Email).IsUnique()`.
- **Міграція** `AddUniqueIndexOnContactEmail` — видаляється старий індекс `IX_Contacts_Email` і створюється той самий індекс з `unique: true`. Оновлено `AppDbContextModelSnapshot` і додано відповідний Designer.

**2. Обробка дублікатів у сервісі**

- **CreateAsync** — перед додаванням контакту перевіряється, чи вже є контакт з таким же email (порівняння без урахування регістру). Якщо є — кидається `ArgumentException` з текстом на кшталт: `"A contact with email '...' already exists."`
- **UpdateAsync** — перед зміною email перев

