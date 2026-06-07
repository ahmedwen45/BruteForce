# BruteForce Password Cracker

A WPF desktop application demonstrating brute-force password recovery using SHA-256 hashing and multi-threading.

---

## 📦 Project Structure

| File | Class | Responsibility |
|---|---|---|
| `PasswordManager.cs` | `PasswordManager` | Password generation, SHA-256+salt hashing, validation |
| `CombinationGenerator.cs` | `CombinationGenerator` | Independently generates all combinations (length 1–6) |
| `BruteForceEngine.cs` | `BruteForceEngine` | Orchestrates multi-thread & single-thread brute force |
| `PerformanceLogger.cs` | `PerformanceLogger` | Records and compares single vs multi-thread performance |
| `MainWindow.xaml` + `.cs` | `MainWindow` | WPF GUI: controls, progress, timer, log output |
| `CustomPasswordDialog.cs` | `CustomPasswordDialog` | Dialog for custom password input |

---

## ✅ Requirements Checklist

- [x] SHA-256 hashing with constant static salt (`BruteForceApp_StaticSalt_2024`)
- [x] Random password length in range [4, 6) → 4 or 5 characters
- [x] Brute force starts from length 1, max 6 — does NOT know password length
- [x] Multi-threading with `Task.Run`, using `(CPU cores - 1)` threads
- [x] Parallel execution — threads work on partitioned index ranges simultaneously
- [x] All threads stop immediately when password is found (CancellationTokenSource)
- [x] Generator and Validator implemented in separate independent classes
- [x] GUI: Start/Stop button, progress bar, elapsed time, found password output
- [x] Performance logging: single-thread vs multi-thread comparison
- [x] Log saved to `performance_log.txt`

---

## 🔀 Commit History

### v1.0 — Core classes
- `PasswordManager.cs` — SHA-256 hashing, salt, random password generation
- `CombinationGenerator.cs` — index-based combination generation, partition support

### v2.0 — Engine & Logger
- `BruteForceEngine.cs` — multi-thread & single-thread attack, CancellationToken, events
- `PerformanceLogger.cs` — comparison report, file output

### v3.0 — GUI & Integration
- `MainWindow.xaml` + `MainWindow.xaml.cs` — full WPF interface
- `CustomPasswordDialog.cs` — custom password input dialog
- `App.xaml` — application entry point

### v4.0 — Final polish & UML
- UML class diagram added
- README finalized
- Project zipped for submission

---

## 🚀 How to Run

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. Open solution in Visual Studio 2022 or run:
   ```
   dotnet run --project BruteForceApp/BruteForceApp.csproj
   ```
3. Click **🎲 Generate Password** to create a target password
4. Click **▶ START ATTACK** to begin brute-force
5. Click **📊 Benchmark** to compare single vs multi-thread performance

---

## 🔐 Security Notes

This project is for **educational purposes only**, demonstrating:
- Importance of password length and complexity
- How hashing + salting protects passwords
- Performance advantage of parallel computation
