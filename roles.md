# Authorization Model — Roles, Role Claims, User Claims

This document defines the authorization model used in the application.  
It standardizes naming conventions for tools, permission levels, and claims.

## Overview

The system uses a hybrid **RBAC + Claims** approach:

- **Roles** provide high-level access levels per tool.
- **Role Claims** define role capabilities (view, read, write, etc.).
- **User Claims** grant specific or exceptional permissions.

## Roles (set via endpoint)

Roles represent high-level access assignments for a specific tool, grant directly to an individual user.

Each role follows a strict naming convention and corresponds to a predefined access level  
(Viewer, Contributor, Moderator, Administrator).

### Format (PascalCase, 2 segments)

**Template**: `Tool`.`Access`

### Template Segments

- **Tool:** Tool name (e.g., `Documents`, `AzureBlobStorage`, `Billing`)
- **Access:** Access level (`Viewer`, `Reader`, `Contributor`, `Moderator`, `Administrator`)

### Examples

- Documents.Viewer
- Documents.Moderator
- AzureBlobStorage.Administrator
- Billing.Contributor
- Ecn.Administrator

## Role Claims (internal, resolved from roles)

Role claims define which capabilities a role provides.

They follow a strict naming convention and are **automatically mapped** based on the role’s access level (Viewer, Contributor, Moderator, Administrator).

### Format (lowercase, 3 segments)

**Template**: role.`tool`.`permission`

### Template Segments

- **role:** Literal prefix `role`
- **tool:** Tool name (lowercase)
- **permission:** Permission name (lowercase)

## Permission Mapping (Automatically Resolved From Role)

| Role Access       | Permissions Granted                                  |
| ----------------- | ---------------------------------------------------- |
| **Viewer**        | `view`                                               |
| **Reader**        | `view`, `read`                                       |
| **Contributor**   | `view`, `read`, `write`                              |
| **Moderator**     | `view`, `read`, `write`, `edit`, `delete`            |
| **Administrator** | `view`, `read`, `write`, `edit`, `delete`, `special` |

### Example Token Claims (Documents.Administrator)

- role.documents.view
- role.documents.read
- role.documents.write
- role.documents.edit
- role.documents.delete
- role.documents.special

## User Claims (set via endpoint)

User claims grant fine-grained, feature-specific, or exceptional permissions directly to an individual user.

They extend or override capabilities without requiring new roles or changes to role definitions.

### Format (lowercase, 3 segments)

**Template:** user.<tool>.<privilege>

### Template Segments

- **user:** Literal prefix `user`
- **tool:** Tool name (lowercase)
- **privilege:** Privilege, feature flag, UI element, or exception rule

### Examples

- user.azureblobstorage.timeline
- user.documents.metadata-tab
- user.documents.experimental-feature

### Common Use Cases

- Enabling or hiding UI tabs
- Feature flags or beta features
- Exception-based permissions
- One-time or temporary access
- Overrides for users outside standard role behavior
