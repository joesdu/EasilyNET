# EASILYNET.SECURITY - CRYPTO ALGORITHMS

## OVERVIEW

Cryptographic algorithms: AES, SM2/SM3/SM4, RSA, RIPEMD, DES, RC4, MD5. No project dependencies — standalone package. SM2/SM3/RIPEMD use BouncyCastle 2.7.0-beta.

## STRUCTURE

```
EasilyNET.Security/
├── AES/           # AES encryption (CBC, PKCS7; AES128/192/256 via AesKeyModel)
├── SM2/           # Chinese SM2 elliptic curve (C1C3C2 default)
├── SM3/           # Chinese SM3 hash
├── SM4/           # Chinese SM4 block cipher (ECB/CBC, 600 lines of overloads)
├── RSA/           # RSA encryption/signing (OaepSHA256, XML/Base64/PEM interop)
├── RIPEMD/        # RIPEMD128/160/256/320
├── DES/           # Triple DES (legacy only)
├── RC4/           # RC4 stream cipher (legacy only)
└── Md5.cs         # MD5 hash
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| AES encryption | `AES/AesCrypt.cs` |
| SM2 sign/encrypt | `SM2/Sm2Crypt.cs` |
| SM3 hash | `SM3/Sm3Signature.cs` |
| SM4 encryption | `SM4/Sm4Crypt.cs` (600 lines — many overloads, not complex) |
| RSA operations | `RSA/RsaCrypt.cs` (key gen, encrypt/decrypt, sign/verify, format conversion) |
| RIPEMD hashing | `RIPEMD/RipeMD128.cs` through `RipeMD320.cs` |

## CONVENTIONS

- All APIs are synchronous (crypto operations are CPU-bound)
- Use `Span<byte>` for buffers to minimize allocations
- Key generation uses secure random (`RandomNumberGenerator`)
- AES/DES/TripleDES internally hash the key — ciphertext is only compatible with this library
- String APIs default to UTF-8 encoding

## ANTI-PATTERNS

- Using weak algorithms (DES, RC4, MD5) for new systems — marked as legacy
- Hardcoding keys/secrets in source
- Not using authenticated encryption (GCM for AES)
- Expecting AES/DES ciphertext to be interoperable with other libraries (internal key derivation)
