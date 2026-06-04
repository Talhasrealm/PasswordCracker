# Password Cracker - Brute Force Demo
Vilnius University | C# OOP Final Assignment

## Version History
- v1.0 - Initial project setup: empty WinForms app
- v1.1 - Add HashHelper: SHA256 hashing with static salt
- v1.2 - Add PasswordManager: random password generation length 4-5
- v1.3 - Add BruteForceEngine: multi-threaded parallel brute force
- v1.4 - Add PerformanceLogger: single vs multi-thread comparison logging
- v1.5 - Add MainForm GUI: app runs and finds password successfully
- v1.6 - Add README and final documentation

## How It Works
1. A random 4-5 character password is generated
2. It is hashed using SHA256 with a static salt
3. Brute force searches all combinations from length 1 to 6
4. Uses (CPU cores - 1) threads in parallel
5. All threads stop immediately when password is found

## Classes
- HashHelper - SHA256 hashing with static salt
- PasswordManager - creates and stores the target password
- BruteForceEngine - multi-threaded brute force attack
- PerformanceLogger - logs and compares performance
- MainForm - WinForms GUI