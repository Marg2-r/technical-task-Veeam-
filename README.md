# Folder Synchronization Tool

A C# console application that performs one-way synchronization between a source directory and a replica directory at a specified time interval.
The application ensures that the replica reflects the contents of the source by copying new files, updating modified files, and removing files and directories that no longer exist in the source.
All synchronization operations are logged to both the console and a log file.

# Implemented Requirements

- **One-way synchronization:** Keeps the replica directory synchronized with the source directory.
- **Periodic synchronization:** Performs synchronization at a user-defined time interval.
- **File and directory management:** Copies new files, updates modified files, and removes files and directories that no longer exist in the source.
- **Logging:** Records file and directory operations in both the console and a log file.
- **Command-line arguments:** Accepts source and replica directory paths, synchronization interval, and log file path as command-line arguments.
- **No third-party dependencies:** Uses built-in .NET libraries for file operations and SHA-256 hash calculation.

# Requirements

.NET 10 SDK

# How to Run

## 1. Clone the repository:
```bash
git clone https://github.com/Marg2-r/technical-task-Veeam.git
```
## 2. Navigate to the project directory:
```bash
cd technical-task-Veeam/TechnicalTask
```
## 3. Run the application:
**Arguments:**

- `source` — Source directory path.
- `replica` — Replica directory path.
- `interval_seconds` — Synchronization interval in seconds.
- `log_path` — Log file path.
```bash
dotnet run -- <source> <replica> <interval_seconds> <log_path>
```
# Example
```bash
dotnet run -- "C:\Sync\Source" "C:\Sync\Replica" 5 "C:\Sync\Logs\sync.log"
```
This command synchronizes the source directory with the replica every 5 seconds and records operations in the console and the specified log file.

Press Ctrl+C to stop the application.

