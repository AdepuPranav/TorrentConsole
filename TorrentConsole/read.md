# BitTorrent Console Client (C#)

A **minimal BitTorrent client written in C#** that can download files directly from peers using the BitTorrent protocol.
This project was built as a learning exercise to understand how torrent clients work internally, including peer communication, piece management, and the download pipeline.

The client currently supports downloading torrents such as Linux ISO images using a basic peer-to-peer workflow.

---

# Project Goals

The purpose of this project is to:

* Understand the BitTorrent protocol at a low level
* Implement peer communication using TCP sockets
* Parse `.torrent` metadata
* Perform handshake validation
* Request and download pieces from peers
* Verify file integrity using SHA-1 hashes
* Reassemble files from downloaded pieces

---

# Current Features

### Torrent Metadata Parsing

* Reads `.torrent` files
* Extracts:

  * file name
  * file size
  * piece length
  * SHA1 piece hashes
  * tracker URLs

### Tracker Communication

* Sends **HTTP announce request**
* Parses tracker response
* Retrieves peer list

### Peer Connection

* TCP connection to peers
* Performs BitTorrent **handshake**
* Validates info hash

### Bitfield Handling

* Receives peer bitfield
* Determines which pieces a peer owns

### Interested / Unchoke

* Sends **INTERESTED** message
* Waits for **UNCHOKE** from peer

### Piece Downloading

* Requests blocks (16 KB)
* Supports **pipelined requests**
* Reassembles blocks into pieces

### Piece Verification

* SHA-1 verification against torrent metadata
* Prevents corrupted downloads

### File Reconstruction

* Writes pieces directly to disk
* Supports out-of-order piece downloads

---

# Project Architecture

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

### Core Components

**PieceManager**

* Tracks downloaded pieces
* Assigns pieces to peers
* Prevents duplicate downloads

**PeerConnection**

* Handles communication with a single peer
* Manages:

  * handshake
  * bitfield parsing
  * block requests
  * piece assembly

**TorrentMetaData**

* Stores torrent information
* Contains piece hashes

**Peer**

* Represents a remote peer
* Stores IP, port, and bitfield

---

# How the Download Process Works

1. Load `.torrent` file
2. Send tracker announce request
3. Receive list of peers
4. Connect to a peer using TCP
5. Perform BitTorrent handshake
6. Receive peer bitfield
7. Send INTERESTED message
8. Wait for UNCHOKE
9. Request blocks of pieces
10. Receive blocks and assemble pieces
11. Verify piece using SHA-1
12. Write verified piece to disk
13. Repeat until file download completes

---

# Requirements

* .NET 7 or newer
* Internet connection
* A `.torrent` file

---

# Setup Instructions

### 1. Clone the Repository

```bash
https://github.com/AdepuPranav/TorrentConsole.git
cd torrent-console-client
```

---

### 2. Restore Dependencies

```bash
dotnet restore
```

---

### 3. Build the Project

```bash
dotnet build
```

---

### 4. Run the Client

```bash
dotnet run
```

---

### 5. Provide a Torrent File

Place a `.torrent` file in the project directory and update the path in:

```
Program.cs
```

Example:

```csharp
var torrentPath = "debian.torrent";
```

Then run the project again.

---

# Example Output

```
Connecting to peer 185.xxx.xxx.xxx:6881
TCP CLIENT CONNECTED
Sending handshake...
Handshake validated
Bitfield received
Sent INTERESTED
Unchoked

Piece 0 verified
Piece 0 saved
Piece 1 verified
Piece 1 saved
```

---

# Current Limitations

This client currently supports only a subset of the BitTorrent protocol.

Not yet implemented:

* UDP trackers
* Magnet link support
* DHT peer discovery
* Peer exchange (PEX)
* Multi-peer piece scheduling
* Endgame mode
* Uploading to other peers
* Encryption

Because of this, some torrents may not work if they rely on these features.

---

# Future Improvements

Planned improvements for the project:

### Networking

* UDP tracker support
* DHT implementation
* Magnet link support

### Performance

* Multi-peer parallel downloads
* Block-level scheduling
* Better pipeline management

### Protocol

* Full message loop
* HAVE message handling
* Peer exchange

### Reliability

* Timeout handling
* Connection retries
* Improved piece verification

---

# Learning Outcomes

This project demonstrates practical understanding of:

* TCP networking
* asynchronous programming in C#
* binary protocol parsing
* distributed peer-to-peer systems
* file reconstruction
* hashing and data verification

---

# Disclaimer

This project is intended **for educational purposes only** to study peer-to-peer networking and distributed protocols.

Use torrents responsibly and respect copyright laws.

---

# Author

Developed as part of a personal learning project to explore how torrent clients work internally.
