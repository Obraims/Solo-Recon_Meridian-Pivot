# StockSync — Inventory Synchronization Prototype

> **NOTE:** This repository represents the **individual Days 1–3 pre-pivot solo reconnaissance prototype** for the Meridian Pivot assignment.
> It demonstrates the core inventory synchronization service built before the Thursday client pivot.
> **No message queues, RabbitMQ, AMQP, badge printing, or Solstice Events Co. components exist in this pre-pivot architecture.**

---

## 1. Problem Statement

Retailers and internal management systems frequently need to keep destination views in sync with warehouse inventory databases.
**StockSync** is a lightweight synchronization prototype that:
1. Reads inventory items from the **Source (Warehouse)** database table.
2. Compares them against the **Destination (Support System)** database table.
3. Detects differences (`UPDATED`, `ADDED`, `UNCHANGED`, `REMOVED`).
4. Copies updated stock values to the destination database.
5. Logs all synchronization events into `sync_history`.
6. Provides a manager dashboard to monitor sync health and trigger inventory synchronization.

---

## 2. Technology Learned

* **Language:** C# (.NET 10)
* **Backend Framework:** ASP.NET Core Minimal API
* **Database:** SQLite (`Microsoft.Data.Sqlite`) using raw ADO.NET (`SqliteConnection`, `SqliteCommand`)
* **Frontend:** Plain HTML5, CSS3, JavaScript (Vanilla JS, Fetch API)
* **Testing:** xUnit (`dotnet test`)

---

## 3. Architecture

```text
Source Inventory (Warehouse)
       ↓
Inventory Sync Service
       ↓
Destination Inventory (Support System)
       ↓
Manager Dashboard
```

---

## 4. Database Schema (`database/stocksync.db`)

### `source_inventory`
* `id` (INTEGER PRIMARY KEY)
* `product_name` (TEXT NOT NULL)
* `quantity` (INTEGER NOT NULL)
* `updated_at` (TEXT NOT NULL)

### `destination_inventory`
* `id` (INTEGER PRIMARY KEY)
* `product_name` (TEXT NOT NULL)
* `quantity` (INTEGER NOT NULL)
* `updated_at` (TEXT NOT NULL)

### `sync_history`
* `id` (INTEGER PRIMARY KEY AUTOINCREMENT)
* `product_id` (INTEGER NOT NULL)
* `product_name` (TEXT NOT NULL)
* `previous_quantity` (INTEGER)
* `new_quantity` (INTEGER)
* `change_type` (TEXT NOT NULL) — `ADDED`, `UPDATED`, `UNCHANGED`, `REMOVED`
* `synced_at` (TEXT NOT NULL)

---

## 5. API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/source/inventory` | List current source (warehouse) inventory |
| `GET` | `/destination/inventory` | List destination inventory |
| `POST` | `/sync` | Trigger synchronization logic & update destination |
| `GET` | `/sync/history` | Retrieve synchronization history audit log |
| `PUT` | `/source/inventory/{id}` | Update quantity of a source inventory product (for demo) |
| `GET` | `/api/status` | Overview of system synchronization state |

---

## 6. How to Run

```powershell
# Navigate to backend folder
cd backend

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run application
dotnet run
```

Then open `http://localhost:5000` in your web browser.

To run automated tests:

```powershell
dotnet test tests/StockSync.Tests.csproj
```

---

## 7. Demonstration Walkthrough

1. **Initial State:**
   - Source: Blue Shoes (20), Black Shirt (15), Red Cap (8)
   - Destination: Blue Shoes (20), Black Shirt (10), Red Cap (8)
   - Dashboard status shows `Black Shirt → Needs Sync`.

2. **Click Sync Inventory:**
   - Synchronization compares Source (15) vs Destination (10).
   - Destination updates Black Shirt quantity to `15`.
   - Dashboard updates status to `In Sync`.
   - History logs: `Black Shirt | 10 → 15 | UPDATED`.

3. **Modify Source Stock:**
   - Select Black Shirt in the demo control, change quantity to `3`, and click **Update Source**.
   - Dashboard shows Black Shirt: Source (3), Destination (15), `Needs Sync`, Stock Level: `Low Stock`.

4. **Re-sync:**
   - Click **Sync Inventory**.
   - Destination updates to `3`.

---

## 8. Learning Notes

- Understood Minimal APIs routing and dependency injection.
- Parameterized SQL execution with raw ADO.NET and `Microsoft.Data.Sqlite`.
- Building idempotent database initialization scripts.
