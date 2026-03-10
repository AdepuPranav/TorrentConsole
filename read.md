# 🚀 BitTorrent Console Client (From Scratch in C#)

![C#](https://img.shields.io/badge/C%23-.NET-blue)
![Platform](https://img.shields.io/badge/Platform-.NET%207+-green)
![Status](https://img.shields.io/badge/Project-Active-brightgreen)
![Purpose](https://img.shields.io/badge/Purpose-Learning%20P2P%20Systems-orange)
![License](https://img.shields.io/badge/License-MIT-lightgrey)

A **BitTorrent client implemented from scratch in C#** to understand how peer-to-peer file sharing works internally.

Instead of using prebuilt libraries, this project manually implements core parts of the **BitTorrent protocol**, including peer discovery, handshake validation, piece downloading, and file reconstruction.

This project focuses on **learning distributed systems, networking protocols, and asynchronous programming** by building a working torrent client step by step.

---

# 🌍 What is BitTorrent?

BitTorrent is a **peer-to-peer (P2P) file sharing protocol** where files are distributed across many users instead of being hosted on a single server.

Each peer downloads **small pieces** of a file from multiple peers simultaneously.

```
      Seeder
        │
  ┌─────┴─────┐
Peer A     Peer B
  │            │
  └─────┬──────┘
        │
     Peer C
```

Each peer both **downloads and uploads pieces**, making the network scalable.

---

# ✨ Features Implemented

## 📦 Torrent Metadata Parsing

Reads `.torrent` files and extracts:

* file name
* file size
* piece length
* SHA-1 piece hashes
* tracker URL

---

## 🌐 Tracker Communication

The client sends an **HTTP announce request** to the tracker.

```
Client → Tracker → Peer List
```

The tracker responds with a **compact list of peers** available for downloading the file.

---

## 🤝 Peer Handshake

Every peer connection starts with the BitTorrent handshake.

```
Client --------------------> Peer
       Handshake

Peer ----------------------> Client
       Handshake
```

The handshake validates:

* protocol string
* reserved bytes
* info hash
* peer id

---

## 🧾 Bitfield Exchange

Peers share which pieces they already have.

```
Peer Bitfield Example

[1 1 0 0 1 0 1 1]

1 = piece available  
0 = piece missing
```

The client uses this information to request only missing pieces.

---

## 📥 Piece Download Pipeline

Files are downloaded **piece by piece**, each piece split into **16KB blocks**.

```
Piece
 ├── Block 0 (16KB)
 ├── Block 1 (16KB)
 ├── Block 2 (16KB)
 └── Block N
```

Workflow:

```
Request Block
      ↓
Receive Block
      ↓
Append to Piece Buffer
      ↓
Repeat until piece complete
```

---

## 🔐 Piece Integrity Verification

Each piece is verified using **SHA-1 hashing**.

```
Downloaded Piece
        │
        ▼
SHA1(piece_data)
        │
        ▼
Compare with torrent metadata
```

If the hash does not match, the piece is **discarded and re-downloaded**.

---

## 💾 File Reconstruction

Once verified, pieces are written to disk at the correct offset.

```
File
 ├── Piece 0
 ├── Piece 1
 ├── Piece 2
 └── Piece N
```

This allows:

* out-of-order downloads
* large file support
* streaming reconstruction

---

# 🏗 Project Architecture

```
TorrentConsole
│
├── Core
│   └── PieceManager.cs
│
├── Models
│   ├── Peer.cs
│   └── TorrentMetaData.cs
│
├── Network
│   ├── PeerConnection.cs
│   ├── HandshakeBuilder.cs
│   └── Messages
│
└── Program.cs
```

---

# 🧠 Key Components

## PieceManager

Responsible for:

* tracking downloaded pieces
* assigning pieces to peers
* preventing duplicate downloads
* scheduling rare pieces first

---

## PeerConnection

Handles communication with peers:

* handshake exchange
* bitfield processing
* block requests
* piece assembly

---

## TorrentMetaData

Stores parsed torrent information:

* file metadata
* piece hashes
* tracker information

---

# ⚙️ Download Workflow

```
Load .torrent file
      │
      ▼
Parse metadata
      │
      ▼
Contact tracker
      │
      ▼
Receive peer list
      │
      ▼
Connect to peer
      │
      ▼
Perform handshake
      │
      ▼
Receive bitfield
      │
      ▼
Send INTERESTED
      │
      ▼
Wait for UNCHOKE
      │
      ▼
Request blocks
      │
      ▼
Receive pieces
      │
      ▼
Verify SHA1
      │
      ▼
Write piece to disk
```

Repeat until download completes.

---

# 📊 Example Console Output

```
Connecting to peer 185.xxx.xxx.xxx:6881

TCP CLIENT CONNECTED
Sending handshake...

Handshake validated
Bitfield received

Sent INTERESTED
Peer UNCHOKED

Piece 0 verified
Piece 0 saved

Piece 1 verified
Piece 1 saved
```

---

# 🛠 Setup Instructions

## 1️⃣ Clone the Repository

```bash
git clone https://github.com/AdepuPranav/TorrentConsole.git
cd torrent-console-client
```

---

## 2️⃣ Restore Dependencies

```
dotnet restore
```

---

## 3️⃣ Build the Project

```
dotnet build
```

---

## 4️⃣ Run the Client

```
dotnet run
```

---

## 5️⃣ Provide a Torrent File

Place a `.torrent` file inside the project directory and update the path in `Program.cs`.

Example:

```csharp
var torrentPath = "debian.torrent";
```

Run the program again to start downloading.

---

# ⚠ Current Limitations

This is a **learning implementation** and does not yet support the full BitTorrent protocol.

Missing features:

* UDP trackers
* Magnet links
* DHT peer discovery
* Peer exchange (PEX)
* Uploading pieces
* Encryption
* Endgame mode
* Advanced piece scheduling

Because of this, some torrents may not work.

---

# 🔮 Future Roadmap

Planned upgrades:

### Networking

* UDP tracker support
* DHT implementation
* magnet link support

### Performance

* multi-peer parallel downloads
* block request pipelining
* improved piece scheduling

### Protocol Support

* HAVE message handling
* peer exchange (PEX)
* extended messaging

### Reliability

* timeout handling
* peer retry system
* improved error recovery

---

# 🎓 Learning Outcomes

This project demonstrates real-world engineering concepts:

* TCP socket networking
* asynchronous programming in C#
* distributed peer-to-peer systems
* binary protocol parsing
* hashing and data integrity
* large file reconstruction

---

# ⚠ Disclaimer

This project is intended **for educational purposes only** to study peer-to-peer networking and distributed systems.

Please respect copyright laws when using torrent technology.

---

# 👨‍💻 Author

Built as a personal learning project to explore **how torrent clients actually work internally** and understand the BitTorrent protocol from the ground up.
