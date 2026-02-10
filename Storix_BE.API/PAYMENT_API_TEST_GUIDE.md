# Payment API - FE Test Guide (MoMo ATM)

Tai lieu nay dung cho team FE test module Payment vua implement.

## 1) Tong quan nhanh

- Muc tieu: company thanh toan 1 lan de unlock full feature.
- Trang thai chinh:
  - `PENDING`: chua unlock
  - `SUCCESS`: da unlock
  - `FAILED`: chua unlock, cho retry
- Payment method hien tai test:
  - `MOMO` (ATM sandbox, khong phai QR)

## 2) API can dung

Base URL (local): `http://localhost:5041`

### Auth required

- Hầu het endpoint payment can JWT Bearer token.
- Token phai co claim `CompanyId`.
- Neu user thao tac payment khac company:
  - HTTP `403`
  - code: `PAY-EX-13`
  - message: `Khong cung cong ty. Ban khong the thao tac payment cua cong ty khac.`

### Danh sach endpoint

- `POST /api/payments`
- `GET /api/payments/status?company_id={companyId}`
- `PUT /api/payments/{paymentId}/success`
- `POST /api/payments/{paymentId}/momo/atm-url`
- `GET /api/payments/momo/atm/callback` (MoMo redirect callback)
- `POST /api/payments/momo/atm/ipn` (MoMo server callback)

## 3) Chuan bi test

### 3.1 Lay token + companyId

Co 2 cach:

1. Dang nhap:
`POST /Login`

```json
{
  "email": "admin.test@example.com",
  "password": "P@ssw0rd123"
}
```

2. Hoac register company moi:
`POST /api/companies/register`

> FE chi can giu `token` va `companyId` tra ve.

### 3.2 Header bat buoc

```
Authorization: Bearer <access_token>
Content-Type: application/json
```

## 4) Flow test chuan cho FE

## Step A - Tao payment pending

`POST /api/payments`

```json
{
  "companyId": 12,
  "amount": 10000,
  "paymentMethod": "MOMO"
}
```

Expected:
- `200 OK`
- `paymentStatus = "PENDING"`
- co `id` (paymentId)

## Step B - Tao MoMo ATM payUrl

`POST /api/payments/{paymentId}/momo/atm-url`

```json
{
  "orderInfo": "Unlock full feature by MoMo ATM test"
}
```

Expected:
- `200 OK`
- co `payUrl`
- FE mo `payUrl` bang browser/tab moi.

## Step C - User thanh toan tren MoMo sandbox

Thẻ test ATM:

- `9704 0000 0000 0018` -> success
- `9704 0000 0000 0026` -> card locked
- `9704 0000 0000 0034` -> insufficient funds
- `9704 0000 0000 0042` -> over limit

Expiry: `03/07`, OTP: `OTP`

## Step D - Poll status de cap nhat UI

`GET /api/payments/status?company_id={companyId}`

Expected:
- Thanh cong: `isUnlocked = true`, `paymentStatus = "SUCCESS"`
- Chua xong: `isUnlocked = false`, `paymentStatus = "PENDING"` hoac `FAILED`

## 5) JSON mau response

### 5.1 status chua thanh toan

```json
{
  "companyId": 12,
  "isUnlocked": false,
  "paymentStatus": "NOT_PAID",
  "paymentId": null,
  "amount": null,
  "paymentMethod": null,
  "paidAt": null
}
```

### 5.2 status pending

```json
{
  "companyId": 12,
  "isUnlocked": false,
  "paymentStatus": "PENDING",
  "paymentId": 1,
  "amount": 10000.0,
  "paymentMethod": "MOMO",
  "paidAt": null
}
```

### 5.3 status success

```json
{
  "companyId": 12,
  "isUnlocked": true,
  "paymentStatus": "SUCCESS",
  "paymentId": 1,
  "amount": 10000.0,
  "paymentMethod": "MOMO",
  "paidAt": "2026-02-10T02:49:27.354288"
}
```

## 6) Error mapping FE can handle

### 6.1 Khac company (quan trong)

HTTP `403`

```json
{
  "code": "PAY-EX-13",
  "message": "Khong cung cong ty. Ban khong the thao tac payment cua cong ty khac."
}
```

### 6.2 Da co payment success roi

HTTP `409`

```json
{
  "code": "PAY-EX-05",
  "message": "Company has already unlocked full feature."
}
```

### 6.3 Company khong ton tai

HTTP `404`

```json
{
  "code": "PAY-EX-08",
  "message": "Company with id 99999 not found."
}
```

### 6.4 Payment id sai

HTTP `409`

```json
{
  "code": "PAY-EX-07",
  "message": "Payment with id 999999 not found."
}
```

## 7) Goi y UI states cho FE

- `NOT_PAID`: show nut `Unlock Full Feature`
- `PENDING`: show badge `Dang cho thanh toan`, cho retry tao payUrl
- `FAILED`: show `Thanh toan that bai`, cho tao payment moi
- `SUCCESS`: an payment banner, mo full feature

## 8) Luu y khi test sandbox

- Neu mo `payUrl` bi trang trang:
  - tao payUrl moi (khong dung lai link cu)
  - mo incognito
  - tat adblock/extension chan script
  - doi browser (Chrome/Edge)
- Callback/IPN tu MoMo co the cham, FE nen poll status.

