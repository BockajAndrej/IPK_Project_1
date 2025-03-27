## [Version 1.0.0] - 2025-03-27

### Changed
- Kód nebol otestovaný na localhost doméne, ale program je plne funkčný pre skenovanie portov.
- Program vytvára len hlavičku pre ICP/IPv4 manuálne. Pre všetky ostatné protokoly necháva operačný systém, aby vytvoril hlavičku miesto nás.

### Known Issues
- Nie je otestované na localhost doméne. Wireshark dane data oznacuje ako "Bad" a program si dane data nevie vyziadat od operacneho systemu. 