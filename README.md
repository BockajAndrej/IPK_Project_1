﻿# IPK 2024/2025 – Project 1: OMEGA – L4 Port Scanner

## Introduction
The goal of the project was to implement an L4 port scanner for TCP and UDP protocols. The program should be capable of determining the status of specified ports on target IP addresses using raw sockets. The program must support scanning addresses via both IPv4 and IPv6.

## Main Features
Port Status Verification – the program can determine whether ports are open, closed, or filtered.

IPv4 and IPv6 Support – the application is compatible with both IP versions.

TCP and UDP Support – implements different scanning methods based on the protocol.

## Core Components
The application is divided into several modules that handle individual functionalities:

1. **Program.cs**
Responsible for the correct output printing of the program and passing parameters between the Parser and CheckPort functions.

```c#
int result = network.CheckPort(targetIpAddress, interfaceName, port, isTcp, waitTime);
```
2. **Global_usings.cs**
Defines commonly used libraries throughout the program.

3. **src/ArgumentProcessing.cs**
Responsible for processing input parameters and validating their correctness via a function called from Program.cs:
```c#
if (argProcess.Parser(args, ref url, ref interfaceName, ref ports, ref waitTime) == false)
                return;
```
4. **src/IPUtilities.cs**
Responsible for overall work with IP addresses such as DNS, DHCP, and filling the IPv4 header.

5. **src/NetworkUtilities.cs**
Implements port scanning via TCP and UDP.
- TCP scan creates an IPv4 (or pseudo IPv6) header and a TCP header. It sends the created byte array and waits for a TCP response.
- UDP scan creates a pseudo IPv4/IPv6 header and a UDP header. It sends the created byte array and waits for an ICMP response.
    - IPv4: If an ICMP packet of type 3 code 3 arrives, the port is defined as closed. Otherwise, it is always defined as open.
    - IPv6: If an ICMP packet of type 1 code 4 arrives, the port is defined as closed. Otherwise, it is always defined as open.

6. **src/TCPUtilities.cs**
Responsible for correctly creating the TCP header along with checksum calculation. It uses pseudo-headers for checksum calculations.

7. **src/UDPUtilities.cs**
Responsible for correctly creating the UDP header along with checksum calculation. It uses pseudo-headers for checksum calculations.

## Protocol Implementation
### TCP SYN Scan
The TCP protocol uses SYN scanning, where a SYN packet is sent via a raw socket to the desired port. The port's status is determined based on the response (ACK flag or RST flag).

- open: The port is open if SYN+ACK is returned.
- closed: The port is closed if an RST response is received.
- filtered: If there is no response, the port is considered filtered.

### UDP Scan
During UDP scanning, the application sends UDP packets and analyzes ICMP responses to determine port status.

- closed: The port is closed if an ICMP "port unreachable" is returned (ICMP Type 3 Code 3 for IPv4, Type 1 Code 4 for IPv6).
- open: If there is no response, the port is considered open.

## Usage
### Formal Syntax
Command to display help output:
```bash
sudo dotnet run --help
```
Formalny zapis prikazu zobrazujuci bezne spustenie programu
```bash
sudo dotnet run [-i interface | --interface interface] [--pu port-ranges | --pt port-ranges | -u port-ranges | -t port-ranges] {-w timeout} [hostname | ip-address]
```
Command to run the program:
```bash
sudo dotnet run -i <interface> -t <tcp_ports> -u <udp_ports> [hostname|IP]
```
Displaying network interfaces:
- This occurs if one of the required program parameters is not provided.

### Program Execution Examples
TCP scan on port 80 for hostname www.vutbr.cz (IPv4/IPv6):
```
dotnet run -i enp0s3 -t 80 www.vutbr.cz
```

# Testing
Tested on the virtual machine `IPK25_Ubuntu24.ova` due to the inability to use the raw socket function on Windows. The network settings for the virtual machine were set to `bridge network`. Testing works on the principle of comparing program output with the reference output from the `nmap` application.

### Hardware:
- Processor: Intel Core i7-1165G7
- Ram: 16 GB LPDDR4x
- Connectivity: Wi-Fi 6 (802.11ax)

### Software:
- Oracle VM VirtualBox
- Ubuntu 64bit
- .NET version: 8.0.406

Testing for localhost was unsuccessful. The program cannot request a packet from the operating system. Wireshark shows that a packet arrived but displays it as **bad**. This causes the program not to function for localhost on its own device.

Tested for hostname = `merlin.fit.vutbr.cz` and `www.vut.cz`

Test results confirmed that the implementation correctly distinguishes between open, closed, and filtered ports for hostnames other than localhost.

# Conclusion
The project was successfully implemented and tested, meeting all assignment requirements. The program allows detecting port status via TCP and UDP protocols, supports both IPv4 and IPv6, and provides the option for detailed output during scanning. The scanning was verified against the `nmap` tool with positive results.

# Tools Used

### Application:
- `nmap` – used to obtain reference outputs (open, closed, filtered).

# Bibliography
- TCP – Transmission Control Protocol
  Postel, J. (1981). Transmission Control Protocol (RFC 793).
  Available at: https://www.rfc-editor.org/rfc/rfc793

- UDP – User Datagram Protocol
  Postel, J. (1980). User Datagram Protocol (RFC 768).
  Available at: https://www.rfc-editor.org/rfc/rfc768

- ICMP – Internet Control Message Protocol
  Internet Engineering Task Force. (1981). Internet Control Message Protocol (RFC 792). Available at: https://www.rfc-editor.org/rfc/rfc792

- Microsoft Docs – Raw Sockets in .NET (System.Net.Sockets)
  Available at: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=net-8.0

- Wikipedia – Port scanner.
  Available at: https://en.wikipedia.org/wiki/Port_scanner
