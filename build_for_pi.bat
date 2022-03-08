@echo OFF
@rem https://s2e-systems.github.io/Rust-RPi4-Windows-Cross-Compilation/
@rem global cargo MUST be edited
@rem version might be different from the article; for me it was 10
cargo build --target=armv7-unknown-linux-gnueabihf %*