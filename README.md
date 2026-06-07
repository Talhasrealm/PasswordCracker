# Password Cracker - Brute Force Demo
Vilnius University | Siauliai Academy | C# OOP Final Assignment

GitHub: https://github.com/Talhasrealm/PasswordCracker

## Version History
- v1.0 - Initial project setup: empty WinForms app
- v1.1 - Add HashHelper: SHA256 hashing with static salt
- v1.2 - Add PasswordManager: random password generation length 4-5
- v1.3 - Add BruteForceEngine: multi-threaded parallel brute force
- v1.4 - Add PerformanceLogger: single vs multi-thread comparison logging
- v1.5 - Add MainForm GUI: app runs and finds password successfully
- v1.7 - Refactor BruteForceEngine: simplified threading logic
- v1.9 - Fix BruteForceEngine and reduce charset to lowercase + numbers
- v2.0 - Simplify GUI: basic Windows style, separate start/stop buttons

## How It Works
1. A random 4-5 character password is generated from a-z and 0-9
2. Password is hashed using SHA256 with a static salt
3. Brute force searches all combinations from length 1 to 6
4. Uses (CPU cores - 1) threads running in parallel
5. All threads stop immediately when password is found
6. Performance comparison between single and multi-thread is logged

## Classes
- HashHelper - SHA256 hashing with static salt
- PasswordManager - creates and stores the target password
- BruteForceEngine - multi-threaded brute force attack engine
- PerformanceLogger - logs and compares single vs multi-thread performance
- MainForm - WinForms GUI with start/stop, progress, timer and results
