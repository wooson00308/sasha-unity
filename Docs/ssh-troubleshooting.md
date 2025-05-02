# Troubleshooting GitHub SSH Connection (`Permission denied (publickey)`)

This document summarizes the steps taken to resolve the `Permission denied (publickey)` error encountered when trying to push to GitHub using a non-default SSH key name (`fork-wooson`) located at `C:\Users\CATZE\.ssh\`.

## Problem

Attempting to push changes to the remote GitHub repository resulted in the following error:

```
git@github.com: Permission denied (publickey).
fatal: Could not read from remote repository.
```

Testing the SSH connection directly also failed:

```
ssh -T git@github.com
git@github.com: Permission denied (publickey).
```

This indicated that Git/SSH was not correctly identifying or using the specified `fork-wooson` key for authentication with `github.com`.

## Troubleshooting Steps

1.  **Initial Push Attempt:** Failed with `Permission denied (publickey)`.
2.  **SSH Connection Test (`ssh -T git@github.com`):** Also failed with the same error, confirming an SSH configuration issue.
3.  **Attempt to Add Key to Agent (`ssh-add C:\Users\CATZE\.ssh\fork-wooson`):** Failed with `Error connecting to agent: No such file or directory`. This suggested the SSH agent service was not running or accessible.
4.  **Attempt to Start SSH Agent Service (`Start-Service ssh-agent` via PowerShell):** Failed, possibly due to permissions or the service not being properly installed/configured on the Windows machine. Direct manipulation of Windows services via terminal proved unreliable.
5.  **Web Search for Solutions:** Searched for methods to force Git/SSH to use a specific key file on Windows. Solutions involving `GIT_SSH_COMMAND` or modifying the SSH config file were found.
6.  **Investigate SSH Config File (`C:\Users\CATZE\.ssh\config`):**
    *   Initial attempts to read the file using `read_file` tool failed (likely due to case sensitivity or tool limitations).
    *   Used `dir C:\Users\CATZE\.ssh` via terminal to confirm the `config` file **did** exist.
    *   Used `type C:\Users\CATZE\.ssh\config` via terminal to view the file contents.

## Solution: Using `.ssh/config`

The `config` file located at `C:\Users\CATZE\.ssh\config` was confirmed to contain the correct configuration to instruct SSH to use the `fork-wooson` key for connections to `github.com`:

```
Host github.com
    HostName github.com
    User git
    IdentityFile C:/Users/CATZE/.ssh/fork-wooson
```

*(Note: `IdentityFile` can use forward slashes `/` even on Windows for SSH config).*

With this configuration correctly in place (despite earlier issues with the `read_file` tool), the SSH connection mechanism could automatically find and use the appropriate key for `github.com`.

## Verification

After confirming the `config` file contents, a subsequent `git push origin main` (and later `git push origin develop`) attempt was **successful**.

## Conclusion

When using non-default SSH key names, the most reliable method to ensure Git/SSH uses the correct key for a specific host (like `github.com`) on Windows is to configure the `~/.ssh/config` (or `C:\Users\<YourUsername>\.ssh\config`) file with the appropriate `Host` and `IdentityFile` directives. Relying on the SSH agent can be problematic if the agent service is not running correctly. 