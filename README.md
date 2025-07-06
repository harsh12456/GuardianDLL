# GuardianDLL 🔒  
**User-Side Threat Detection for DLL Hijacking and Anomalous Behavior**

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-brightgreen)

---

## 🚀 Overview

**GuardianDLL** is a Windows-based application built in **C# (WPF)** that empowers end users to detect and analyze suspicious DLLs on their system — including those that are unsigned, tampered, or commonly exploited in **DLL hijacking** attacks.

Unlike traditional security models that rely heavily on software developers for safe loading practices, GuardianDLL shifts prevention responsibility to the **user side**, giving individuals visibility into what DLLs are present and whether they pose a threat.

---

## 🧠 Key Features

- ✅ **Full C:\ Drive DLL Scan** with safe access handling
- 🧾 **Digital Signature Validation** (signed, unsigned, invalid certs)
- 🧠 **Anomaly & Heuristic Detection**
  - Suspicious folder locations (Temp, AppData, etc.)
  - Malicious-looking DLL names (e.g. `xworm`, `patch`, `cloudflare`)
- 🧱 **Known Hijackable DLL Lookup**
  - Referenced from a configurable list of high-risk DLLs
- 📋 **Suspicious DLL Report UI**
  - With filtering, timestamping, and live scanning feedback
- ⚙️ **Runs with Admin Privileges** for complete system visibility

---

## 🎓 Educational Use

This project is designed for **students and cybersecurity beginners** to explore:

- How DLL hijacking works
- The importance of digital signatures
- Real-world threat analysis techniques
- Windows internals & system scanning

GuardianDLL is a powerful entry-level tool to teach secure computing and threat awareness from a user’s perspective.

---

## 🛠️ Tech Stack

- **Language:** C# (.NET 8 / .NET Framework)
- **Framework:** WPF (Windows Presentation Foundation)
- **UI Components:** ListBox, TabControl, TextBlock
- **Security APIs:** `X509Certificate`, file signature checks
- **Asynchronous Scanning:** `Task.Run`, dispatcher-safe UI updates

---

## 📷 Screenshots

> *(Include images of the UI, loading animation, suspicious DLL list, and filtering panel here)*

---

## 🧪 How It Works

1. On startup, the app requests **administrator privileges**
2. Scans all `.dll` files in the system (C:\)
3. Each DLL is evaluated for:
   - Valid digital signature
   - Path heuristics (Temp, Roaming, Updater, etc.)
   - Hijack-prone names (`version.dll`, `winmm.dll`, etc.)
4. Suspicious DLLs are shown in a separate tab
5. Optional: export results, filter by type

---

## 🧷 Sample Suspicious Detections

| DLL Path | Status |
|----------|--------|
| `C:\Users\Public\AppData\Local\Temp\cloudflare_update.dll` | 🛑 UNSIGNED |
| `C:\ProgramData\NVIDIA\drivers\nvTelemetry64.dll` | ⚠️ INVALID CERT |
| `C:\Users\Sanskar\AppData\Roaming\SystemCache\xworm_helper.dll` | 🧪 HEURISTIC MATCH |

---

## 📂 Folder Structure

```

GuardianDLL/
├── GuardianDLL.sln
├── MainWindow\.xaml / .cs
├── SuspiciousDLL.xaml / .cs
├── Resources/
│   └── known\_hijack\_dlls.json
├── App.config / App.xaml
└── README.md

````

---

## 🔐 Why This Matters

Most modern malware abuses **DLL search order hijacking** to inject malicious code through unsigned or tampered DLLs. GuardianDLL helps users:

- Spot vulnerable or suspicious libraries
- Understand how malicious DLLs hide
- Learn to verify binaries and certs manually

---

## 📦 Installation

1. Clone the repo:
   ```bash
   git clone https://github.com/yourusername/GuardianDLL.git

2. Open the `.sln` in Visual Studio
3. Run as **Administrator**
4. Start scanning!

---

## 📌 Future Enhancements

* [ ] Export scan results to CSV
* [ ] Real-time DLL monitoring
* [ ] Integration with VirusTotal or hash reputation DB
* [ ] Trust-level scoring system
* [ ] Automatic quarantine or blocking suggestions

---

## 👥 Contributors

* [Sanskar Srivastava](https://github.com/Esquire31) – Lead Developer, Research & Design
* [Harsh Patel](https://github.com/harsh12456) – Frontend Developer, Design
---

## 📃 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 🙌 Acknowledgements

* [Microsoft Docs – Authenticode](https://learn.microsoft.com/en-us/windows/win32/seccrypto/about-authenticode)
* Malware researchers and contributors in DLL Hijacking datasets
* Inspiration from real-world threats like **XWorm**, **AsyncRAT**, and **DLL Sideloading** case studies

---

