# LEMP – Local Energy Management Platform for RevPi Connect 5 (.NET 8)

##  English

This project is a modular, scalable REST API-based system designed for local energy monitoring and control using the RevPi Connect 5 industrial computer.
It supports data collection from smart meters, inverters, and battery systems via RS485, and is capable of forwarding timestamped measurements to external systems like KEP over HTTPS.

### 🔧 Key Features

- .NET 8 Web API with clean architecture
- SQLite/PostgreSQL-based data storage
- RS485/Modbus sensor integration
- JSON data push to KEP (with 5 and 15-minute aggregation)
- Modular design for future extensions (CAN FD, machine learning, UI dashboard)
- HTTPS-ready, JWT authentication (planned)
- ARM64-compatible for RevPi Connect 5

### 📁 Project Structure


LEMP-Connect5-Platform/  
├── LEMP.Api             # ASP.NET Core Web API  
├── LEMP.Application     # DTOs, interfaces, business logic  
├── LEMP.Infrastructure # DB access, Modbus, serial integration  
├── LEMP.Domain          # Core domain models and entities  
├── LEMP.Tests           # NUnit unit tests  
└── README.md            # Project documentation

### ⚙️ Requirements

- RevPi Connect 5 (ARM64)
- Linux (Ubuntu 22.04 or RevPi OS)
- .NET 8 SDK + Runtime
- SQLite or PostgreSQL
- RS485 interface (built-in or USB converter)

### 🚀 Getting Started

git clone https://github.com/<your-username>/LEMP-Connect5-Platform.git
cd LEMP-Connect5-Platform
dotnet build
dotnet run --project LEMP.Api

Access the API at: https://localhost:5001/swagger

### 📄 License

MIT License

### 👤 Author

Ecocell Solar Energy – Developed by Peter Kresz

---

##  Magyar

Ez a projekt egy moduláris, skálázható, REST API-alapú rendszer, amelyet a RevPi Connect 5 ipari számítógépre terveztünk helyi energiamenedzsment céljából.
A rendszer RS485-ön keresztül gyűjt adatokat okosmérőkből, inverterekből és akkumulátorokból, majd időbélyeggel ellátva továbbítja őket KEP kompatibilis rendszer felé HTTPS-en keresztül JSON formátumban.

### 🔧 Fő funkciók

- .NET 8 Web API, tiszta architektúrával
- SQLite/PostgreSQL alapú adattárolás
- RS485/Modbus eszköz integráció
- JSON adatküldés KEP felé (5 és 15 perces aggregációval)
- Moduláris kialakítás a jövőbeli bővítésekhez (CAN FD, gépi tanulás, UI dashboard)
- HTTPS támogatás, JWT hitelesítés (tervben)
- ARM64-kompatibilis a RevPi Connect 5 platformhoz

### 📁 Projektstruktúra

LEMP-Connect5-Platform/  
├── LEMP.Api             # ASP.NET Core Web API  
├── LEMP.Application     # DTO-k, interfészek, üzleti logika  
├── LEMP.Infrastructure # Adatbázis-kezelés, Modbus, soros kommunikáció  
├── LEMP.Domain          # Alap entitások, modellek  
├── LEMP.Tests           # NUnit unit tesztek  
└── README.md            # Dokumentáció

### ⚙️ Követelmények

- RevPi Connect 5 (ARM64)
- Linux (Ubuntu 22.04 vagy RevPi OS)
- .NET 8 SDK + Runtime
- SQLite vagy PostgreSQL
- RS485 interfész (beépített vagy USB-s)

### 🚀 Első lépések

git clone https://github.com/<your-username>/LEMP-Connect5-Platform.git
cd LEMP-Connect5-Platform
dotnet build
dotnet run --project LEMP.Api

Swagger elérés: https://localhost:5001/swagger

### 📄 Licenc

MIT Licenc

### 👤 Készítette

Ecocell Solar Energy – Fejlesztő: Peter Kresz
