# 🍽️ Restaurant System Simulation (C#, .NET)

A console-based distributed restaurant simulation developed in **C# (.NET)** that demonstrates
**socket-based communication** and **multithreaded processing**.

The system models real-world restaurant operations by separating responsibilities into
independent roles (Server, Manager, Waiter, Bartender, Cook), each running as a standalone
console application and communicating over a network.

This project was created as an academic exercise to explore:
- Client–server architecture
- TCP socket communication
- Concurrent processing with multithreading
- Coordination between multiple independent processes

---

## 🔧 Technologies Used

- C#
- .NET
- TCP Sockets
- Multithreading
- Console Applications
- Visual Studio

## ▶️ How to Run the Application

This system consists of multiple console applications that must be started in a specific order.

### Prerequisites
- Visual Studio
- .NET installed
- Solution opened in Visual Studio

### Step 1: Configure Multiple Startup Projects

1. Open the solution in **Visual Studio**
2. Right-click the **solution** → **Properties**
3. Go to **Startup Project**
4. Select **Multiple startup projects**
5. Set the following projects to **Start**:
   - Bartender
   - Waiter
   - Cook
   - Manager
6. Click **OK**
7. Run the solution (**F5**)

⚠️ An exception may occur at this stage — **this is expected and can be ignored**.

### Step 2: Start the Server

1. Go back to **Solution Properties**
2. Select **Single startup project**
3. Choose **Server**
4. Run the solution (**F5**)

Once the server is running, all components will begin communicating and the system
will function correctly.

---

## 👨‍🍳 System Roles & Functionality

### 🧾 Waiter

The waiter is responsible for handling customer interactions and table operations.

Available options:
1. **Zauzmi novi sto** (Occupy a new table)
2. **Izdaj račun** (Issue a bill)
3. **Rezervacija** (Make a reservation)
0. **Zatvori konobara** (Close waiter application)

---

### 🧑‍💼 Manager

The manager handles reservations and oversight tasks.

Available options:
1. **Napravi rezervaciju** (Create a reservation)
2. **Proveri rezervaciju** (Check a reservation)
0. **Ugasi menadžera** (Shut down manager)

---

### 🍺 Bartender

The bartender simulates drink preparation.
- Receives drink orders via socket communication
- Processes requests concurrently using multithreading
- Operates automatically without user input

---

### 🍳 Cook

The cook simulates food preparation.
- Receives food orders from the system
- Processes multiple orders in parallel
- Runs automatically to demonstrate concurrent workload handling

---

## 🧠 Notes

- Each role runs as an independent process
- Communication is handled using TCP sockets
- Multithreading enables parallel order processing
- The system demonstrates real-world distributed application behavior
