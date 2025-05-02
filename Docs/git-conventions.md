# Git Conventions

This document outlines the Git conventions used in the sasha-unity project to maintain consistency and clarity in the version control history.

## 1. Branching Strategy

We follow a simplified Gitflow-like model:

-   **`main`**: Represents the **strictly stable, production-ready codebase**. This branch is primarily a **target** for merging completed `release` or `hotfix` branches. Direct commits or feature merges into `main` are **forbidden**.
-   **`develop`**: The main integration branch for ongoing development. All feature branches (`feat/...`, `fix/...`, etc.) originate from `develop` and are merged back into `develop`. `develop` should always aim to be in a stable, runnable state, incorporating changes from completed features and necessary updates from releases/hotfixes.
-   **Feature Branches (`<type>/<scope>/<short-description>`)**: For developing new features or non-urgent bug fixes. Branched from `develop` and merged back only into `develop`.
-   **Release Branches (`release/<version>`)**: To prepare for a new production release. Branched from `develop`. Allows for final testing, documentation, and minor bug fixes. Upon completion, merged into `main` (and tagged) **AND** also merged back into `develop` to ensure release preparations are reflected in future development.
-   **Hotfix Branches (`hotfix/<issue-description>`)**: For critical bug fixes discovered in production. Branched directly from the relevant commit on `main`. Upon completion, merged into `main` (and tagged) **AND** also merged back into `develop` to ensure the fix is included in ongoing development.

## 2. Branch Naming Convention

Feature, release, and hotfix branches should follow a hierarchical structure using forward slashes (`/`).

**Format Examples:**

-   `feat/combat/add-counter-attack` (Branched from `develop`)
-   `fix/ui/button-alignment` (Branched from `develop`)
-   `release/v1.1.0` (Branched from `develop`)
-   `hotfix/login-crash` (Branched from `main`)
-   `feat/ai/ml-agents-pilot` (Branched from `develop`)

*(See Commit Convention `<type>` for common branch prefixes like `feat`, `fix`, `refactor`, etc.)*

## 3. Commit Message Convention

Commit messages should follow the Conventional Commits specification. This makes the commit history more readable and enables automated changelog generation.

**Format:**

```
<type>(<scope>): <short summary>

[optional body]

[optional footer(s)]
```

-   **`<type>`:** Must be one of the following:
    *   `feat`: A new feature (correlates with minor version bumps).
    *   `fix`: A bug fix (correlates with patch version bumps).
    *   `refactor`: Code changes that neither fix a bug nor add a feature.
    *   `perf`: Code changes that improve performance.
    *   `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc).
    *   `test`: Adding missing tests or correcting existing tests.
    *   `build`: Changes that affect the build system or external dependencies (e.g., Unity version, packages).
    *   `ci`: Changes to CI configuration files and scripts.
    *   `docs`: Documentation only changes.
    *   `chore`: Other changes that don't modify `src` or `test` files (e.g., updating dependencies, project settings).
-   **`<scope>`:** (Optional) The scope should be the name of the module/component affected (e.g., `combat`, `ui`, `core`, `excel`, `pilot-ai`). Use lowercase.
-   **`<short summary>`:**
    *   Use the imperative, present tense: "change" not "changed" nor "changes".
    *   Don't capitalize the first letter.
    *   No dot (`.`) at the end.
    *   Keep it concise (max 50 characters recommended).
-   **`[optional body]`:**
    *   Use the imperative, present tense.
    *   Include motivation for the change and contrast this with previous behavior.
    *   Separate subject from body with a blank line.
    *   Wrap the body at 72 characters.
-   **`[optional footer(s)]`:**
    *   Reference issues or pull requests (e.g., `Closes #123`, `Refs #456`).
    *   Include breaking change information (`BREAKING CHANGE: description`). A commit with `BREAKING CHANGE:` in the footer correlates with a major version bump.

**Examples:**

```
feat(combat): add repair action for support pilots

Introduces RepairSelf and RepairAlly actions. Support pilots will now prioritize repairing damaged allies or themselves if needed.

Refs #42
```

```
fix(ui): prevent null reference in CombatTextUIService

The service could throw an error if combat ended before any logs were generated. Added null checks to prevent this.

Closes #55
```

## 4. General Guidelines

-   **`main` is Sacred**: Never push directly to `main`. Only merge `release` or `hotfix` branches into `main` after thorough testing and approval.
-   **Work from `develop`**: Always branch off from the `develop` branch for new features and regular bug fixes.
-   **Keep `develop` Stable**: Ensure code merged into `develop` is functional and doesn't break the build or introduce major regressions.
-   **Atomic Commits:** Each commit should represent a single logical change. Avoid mixing unrelated changes in one commit.
-   **Clear Descriptions:** Write clear and concise commit messages. The body should explain *what* changed and *why*, not just *how*.
-   **Rebase/Squash Feature Branches:** Before merging **feature branches into `develop`**, consider rebasing onto the latest `develop` and squashing related commits for a cleaner history. Avoid squashing on `develop` or `main` itself.
-   **Push Frequently:** Push your local changes to the remote repository often, especially when working on feature branches.
-   **Delete Merged Feature Branches:** Once a **feature branch** has been successfully merged into **`develop`**, delete the local branch (`git branch -d <branch-name>`) and the remote branch (`git push origin --delete <branch-name>`) to keep the repository clean. Release and hotfix branches might be kept longer for reference if needed. 