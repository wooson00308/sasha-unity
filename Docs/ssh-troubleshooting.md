# Troubleshooting GitHub SSH Connection (`Permission denied (publickey)`)

This document summarizes the steps taken to resolve the `Permission denied (publickey)` error encountered when trying to push to GitHub using a **non-default SSH key name** (e.g., `my_github_key` instead of `id_rsa`) located in the standard `.ssh` directory (typically `C:\Users\<YourUsername>\.ssh\` on Windows).

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

This indicated that Git/SSH was not correctly identifying or using the **specified non-default SSH key** for authentication with `github.com`.

## Troubleshooting Steps

1.  **Initial Push Attempt:** Failed with `Permission denied (publickey)`.
2.  **SSH Connection Test (`ssh -T git@github.com`):** Also failed with the same error, confirming an SSH configuration issue.
3.  **Attempt to Add Key to Agent (`ssh-add <path-to-your-.ssh-folder>/<your-non-default-key-name>`):** This might fail with `Error connecting to agent: No such file or directory` if the SSH agent service is not running or accessible.
4.  **Attempt to Start SSH Agent Service (e.g., `Start-Service ssh-agent` via PowerShell):** This might also fail due to permissions or service configuration issues on Windows. Direct manipulation of Windows services via terminal can be unreliable.
5.  **Web Search for Solutions:** Searched for methods to force Git/SSH to use a specific key file on Windows. Solutions involving `GIT_SSH_COMMAND` or modifying the SSH config file were found.
6.  **Investigate SSH Config File (`<path-to-your-.ssh-folder>/config`):**
    *   Confirm if the `config` file exists in your `.ssh` directory (e.g., using `dir <path-to-your-.ssh-folder>` in CMD/PowerShell).
    *   If it exists, view its contents (e.g., using `type <path-to-your-.ssh-folder>/config`).
    *   If it doesn't exist, create it.

## Solution: Using `.ssh/config`

The most reliable method is to explicitly tell SSH which key to use for `github.com` by creating or editing the `config` file in your `.ssh` directory (e.g., `C:\Users\<YourUsername>\.ssh\config`). Add or ensure the following block exists, replacing placeholders with your actual values:

```
Host github.com
    HostName github.com
    User git
    # Use forward slashes for the path
    IdentityFile <path-to-your-.ssh-folder>/<your-non-default-key-name>
    # Example:
    # IdentityFile C:/Users/YourUsername/.ssh/my_github_key
```

*(Note: `IdentityFile` typically uses forward slashes `/` for paths in SSH config files, even on Windows).*

With this configuration in place, the SSH connection mechanism should automatically find and use the appropriate key for `github.com`.

## Verification

After creating or correcting the `config` file, attempt the `git push` or `ssh -T git@github.com` command again. It should now succeed.

## Conclusion

When using non-default SSH key names, the most reliable method to ensure Git/SSH uses the correct key for a specific host (like `github.com`) on Windows is to configure the `~/.ssh/config` (or `C:\Users\<YourUsername>\.ssh\config`) file with the appropriate `Host` and `IdentityFile` directives. Relying on the SSH agent can be problematic if the agent service is not running correctly. 