# Error Codes

## Purpose

This document defines stable application error codes.

The codes are technology-independent and are used consistently by the ASP.NET Core API.

---

## Authentication

| Code    | Description           |
| ------- | --------------------- |
| AUTH001 | Invalid credentials   |
| AUTH002 | Access token expired  |
| AUTH003 | Refresh token expired |
| AUTH004 | Unauthorized          |

---

## User

| Code    | Description             |
| ------- | ----------------------- |
| USER001 | User not found          |
| USER002 | Email already exists    |
| USER003 | Username already exists |

---

## Validation

| Code   | Description       |
| ------ | ----------------- |
| VAL001 | Validation failed |
| VAL002 | Invalid request   |

---

## Category

| Code   | Description        |
| ------ | ------------------ |
| CAT001 | Category not found |

---

## Product

| Code   | Description       |
| ------ | ----------------- |
| PRO001 | Product not found |

---

## System

| Code   | Description           |
| ------ | --------------------- |
| SYS001 | Internal server error |
| SYS002 | Database error        |
| SYS003 | Unexpected error      |

---

## Usage Rules

* Do not reuse an existing code for a different meaning.
* Add new codes when new domain errors are introduced.
* Map exceptions to these codes in the centralized exception handler.
* Do not expose stack traces or sensitive implementation details in production responses.
