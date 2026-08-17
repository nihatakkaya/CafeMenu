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
| USER004 | User setup token invalid |
| USER005 | User setup already completed |

---

## Validation

| Code   | Description       |
| ------ | ----------------- |
| VAL001 | Validation failed |
| VAL002 | Invalid request   |

---

## Category

| Code   | Description              |
| ------ | ------------------------ |
| CAT001 | Category not found       |
| CAT002 | Category reorder invalid |

---

## Cafe

| Code    | Description              |
| ------- | ------------------------ |
| CAFE001 | Cafe not found           |
| CAFE002 | Cafe slug already exists |
| CAFE003 | Cafe inactive            |

---

## Cafe Membership

| Code   | Description                    |
| ------ | ------------------------------ |
| MEM001 | Cafe membership not found      |
| MEM002 | Cafe membership already exists |

---

## Tenant Authorization

| Code      | Description             |
| --------- | ----------------------- |
| TENANT001 | Tenant access forbidden |

---

## Product

| Code   | Description                           |
| ------ | ------------------------------------- |
| PRO001 | Product not found                     |
| PRO002 | Product invalid category relationship |
| PRO003 | Product reorder invalid               |

---

## Image Upload

| Code   | Description              |
| ------ | ------------------------ |
| IMG001 | Unsupported image format |
| IMG002 | Invalid image content    |
| IMG003 | Image too large          |
| IMG004 | Image storage failed     |

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
