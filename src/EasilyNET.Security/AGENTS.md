# EASILYNET.SECURITY - CRYPTO ALGORITHMS

## OVERVIEW

Cryptographic algorithms: AES, SM2/SM3/SM4, RSA, RIPEMD, DES, RC4, MD5.

## STRUCTURE

```
EasilyNET.Security/
├── AES/           # AES encryption (CBC, GCM)
├── SM2/           # Chinese SM2 elliptic curve
├── SM3/           # Chinese SM3 hash
├── SM4/           # Chinese SM4 block cipher
├── RSA/           # RSA encryption/signing
├── RIPEMD/        # RIPEMD128/160/256/320
├── DES/           # Triple DES
├── RC4/           # RC4 stream cipher
└── Md5.cs         # MD5 hash
```

## WHERE TO LOOK

| Task             | Location              |
| ---------------- | --------------------- |
| AES encryption   | `AES/AesCrypt.cs`     |
| SM2 sign/encrypt | `SM2/Sm2Crypt.cs`     |
| SM3 hash         | `SM3/Sm3Signature.cs` |
| SM4 encryption   | `SM4/Sm4Crypt.cs`     |
| RSA operations   | `RSA/RsaCrypt.cs`     |

## CONVENTIONS

- All APIs are synchronous (crypto operations are CPU-bound)
- Use `Span<byte>` for buffers to minimize allocations
- Key generation uses secure random (RandomNumberGenerator)

## ANTI-PATTERNS

- Using weak algorithms (DES, RC4, MD5) for new systems
- Hardcoding keys/secrets in source
- Not using authenticated encryption (GCM for AES)
