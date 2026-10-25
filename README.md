# Salefny (سلفني)

### Need It? Rent It. Return It.

Salefny is an online platform built on the Sharing Economy model, similar to Airbnb, but focused on tools, equipment, and personal or professional assets instead of real estate. It provides a secure framework for peer-to-peer asset sharing, allowing owners to benefit from unused items while giving renters budget-friendly access to what they need.

---

## Table of Contents

1. [Project Name](#1-project-name)
2. [Project Idea](#2-project-idea)
3. [Team Members](#3-team-members)
4. [Work Plan](#4-work-plan)
5. [Roles & Responsibilities](#5-roles--responsibilities)
6. [KPIs](#6-kpis)
7. [Instructor](#7-instructor)
8. [Project Files](#8-project-files)

---

## 1. Project Name

**Salefny (سلفني)** is Arabic for “lend me”. The name reflects the platform’s core idea: enabling people to lend and borrow useful items within a community, reducing the need for everyone to buy things they rarely use. It represents a culture of trust, sharing, and mutual benefit.

---

## 2. Project Idea

### The Problem

- Buying equipment for rare, one-time use is expensive and wasteful.
- Valuable assets may remain unused at home and generate no value.
- There is no trusted and structured way to rent items from other individuals.

### The Solution

Salefny works like Airbnb, but for tools, cameras, party supplies, camping gear, and other personal or professional assets instead of real estate.

The platform aims to give renters budget-friendly access to items, allow owners to generate passive income from unused assets, and provide a secure framework through identity verification, deposits, ratings, and reviews.

### How It Works

| Step | What happens |
|---|---|
| **1. List** | An owner publishes an item with photos, a daily price, a refundable deposit, a city, and available dates. |
| **2. Find** | A renter searches by keyword, category, city, and price and checks the availability calendar. |
| **3. Book** | The renter sends a booking request. The owner approves or rejects the request. |
| **4. Use & Review** | The item is handed over, used, and returned. Both sides can rate each other after completion. |

### Key Features

| Renters | Owners | Admins |
|---|---|---|
| Smart search by category, city, and price | Listing management with photos, pricing, and deposits | Identity verification |
| Interactive booking calendar | Approve or reject booking requests | User management |
| Price and deposit breakdown | Availability management | Category management |
| Ratings and reviews | Manage item listings | Analytics dashboard |

### Booking Lifecycle

`Pending` → `Approved` → `Active` → `Completed`

Other possible states include:

`Rejected` · `Cancelled` · `Disputed`

### Trust and Safety

- Identity verification before listing or booking
- Owner approval for booking requests
- Refundable security deposit
- Double-booking prevention
- Ratings and reviews
- Admin moderation of users and listings

### Technology Stack

| Layer | Technology |
|---|---|
| Frontend | HTML5, CSS3, JavaScript, Bootstrap 5, Razor Views |
| Backend | ASP.NET Core MVC (C#) |
| Data Access | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication | ASP.NET Core Identity |
| Testing | xUnit, Moq |
| Version Control | Git and GitHub |

### Architecture

The planned solution follows a layered architecture with separation of concerns:

- **Web (Presentation):** MVC controllers, Razor views, ViewModels, and static files
- **Application:** Business services, validation, and business rules
- **Domain:** Entities and enums
- **Infrastructure:** EF Core DbContext, migrations, file storage, and email services

---

## 3. Team Members

| # | Name | Role |
|---|---|---|
| 1 | Clara Maged Abdalla Makary | Project Manager / Scrum Master |
| 2 | Mai Ashraf Abdallah Hassan | Backend Developer |
| 3 | Nourhan Ramadan Hassan Hamshary | Frontend Developer & UI/UX Designer |
| 4 | Shorouk Hany Mostafa Mohamed | Database Designer |
| 5 | Rovana Sameh Sami Sorour | QA / Tester |

---

## 4. Work Plan

**Methodology:** Agile (Scrum-style) with one-week sprints. Each sprint is planned to end with a working increment, a review, and a retrospective. Tasks are tracked using a Kanban board with the following stages:

`Backlog` → `In Progress` → `In Review` → `Done`

| Sprint | Goal | Done When |
|---|---|---|
| **1** | **Foundation:** Solution structure, SQL Server setup, Identity with roles, seed data, and base layout | A user can register, log in, and log out; the admin account exists |
| **2** | **Verification & Profiles:** Profile page, ID upload, and admin verification queue | An admin approves an ID and the user becomes verified |
| **3** | **Listings:** Create, edit, and hide items, image upload, and categories | A verified owner publishes an item with photos |
| **4** | **Discovery:** Search, filters, sorting, paging, item details, and public profile | A guest finds an item in under three clicks |
| **5** | **Booking Core:** Availability calendar, date blocks, overlap check, and price calculation | A booking request is saved and overlapping requests are refused |
| **6** | **Booking Workflow & Reviews:** Approve, reject, cancel, handover, complete, reviews, and notifications | A full booking runs from request to review |
| **7** | **Admin & Analytics:** User and category management, moderation, and dashboard | The admin sees correct numbers and can suspend a user |
| **8** | **Hardening & Delivery:** Validation, error pages, responsive fixes, testing, demo video, and presentation | All test cases pass and the final package is submitted |

---

## 5. Roles & Responsibilities

| Role | Responsibility |
|---|---|
| **Project Manager / Scrum Master** | Planning, sprint coordination, and deadline tracking |
| **Backend Developer(s)** | Controllers, services, EF Core, and business logic |
| **Frontend Developer(s)** | Razor views, Bootstrap, JavaScript, and calendar |
| **Database Designer** | ERD, database schema, migrations, and seed data |
| **QA / Tester** | Test cases, bug reports, and regression testing |
| **UI/UX Designer** | Wireframes, mockups, and style guide |

---

## 6. KPIs

| KPI | Target |
|---|---|
| Core features (FR-01 to FR-15) | All working end to end |
| Double bookings in testing | 0 |
| Time for a new tester to complete a booking | Under 5 minutes |
| Time for an owner to create a listing | Under 5 minutes |
| Time for an admin to process an ID verification | Under 1 minute |
| Main page load time | Under 3 seconds |
| Responsiveness | Mobile, tablet, and desktop |
| Final delivery | Code, documentation, demo video, and presentation delivered on time |

---

## 7. Instructor

| | |
|---|---|
| **Instructor** | Ayed Ali |
| **Programme** | DEPI Graduation Project |

---

## 8. Project Files

### Current Files

- [Project Documentation](https://docs.google.com/document/d/18tkH7eESsXnJT8j5Ya_GWfg2zppu2I_ksb1NkMlsyV0/edit?usp=sharing) — Project planning, stakeholder analysis, database design, requirements, and UI/UX documentation.

### Planned Project Structure

```text
Salefny/
├── README.md
├── docs/
│   └── [SALEFNI_Graduation_Project_Documentation.docx](https://docs.google.com/document/d/18tkH7eESsXnJT8j5Ya_GWfg2zppu2I_ksb1NkMlsyV0/edit?usp=sharing)
└── src/
    └── ASP.NET Core MVC application
```

> **Note:** The `src/` folder and source code will be added when development starts.

---

# Salefny

Need It? Rent It. Return It.
