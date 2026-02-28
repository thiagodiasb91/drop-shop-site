
  stateDiagram
  direction TB

  [*] --> AUTHENTICATED

  AUTHENTICATED --> NO_ACCESS: role = new-user
  AUTHENTICATED --> ACTIVE: role = admin

  AUTHENTICATED --> SETUP_REQUIRED: role = seller && resource_id == null
  AUTHENTICATED --> SETUP_REQUIRED: role = supplier && incomplete

  SETUP_REQUIRED --> ACTIVE: setup concluído

  ACTIVE --> [*]
