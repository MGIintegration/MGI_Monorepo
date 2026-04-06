#!/usr/bin/env python3
"""
Generate interactive HTML diagrams for MGI Deep Management System Integration
Shows DFD, system architecture, and detailed flows
"""

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MGI System Integration Diagrams</title>
    <script src="https://cdn.jsdelivr.net/npm/mermaid@10.6.1/dist/mermaid.min.js"></script>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }}
        .container {{
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }}
        h1 {{
            color: #2c3e50;
            border-bottom: 3px solid #3498db;
            padding-bottom: 10px;
        }}
        h2 {{
            color: #34495e;
            margin-top: 40px;
            border-left: 4px solid #3498db;
            padding-left: 15px;
        }}
        .diagram-container {{
            background: white;
            padding: 20px;
            border: 1px solid #ddd;
            border-radius: 5px;
            margin: 20px 0;
            overflow-x: auto;
        }}
        .mermaid {{
            text-align: center;
        }}
        .description {{
            background: #ecf0f1;
            padding: 15px;
            border-radius: 5px;
            margin: 15px 0;
            line-height: 1.6;
        }}
        .note {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px 15px;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class="container">
        <h1>MGI Deep Management System Integration - System Diagrams</h1>
        
        <div class="description">
            <strong>Overview:</strong> These diagrams show the data flow, system architecture, and integration points 
            for the MGI Deep Management System. All systems work offline with local storage, communicating through 
            event-based synchronization.
        </div>

        <h2>1. System Architecture & Cross-Team Dependencies</h2>
        <div class="diagram-container">
            <div class="mermaid">
{system_architecture}
            </div>
        </div>
        <div class="description">
            <strong>Key Points:</strong>
            <ul>
                <li><strong>Economy</strong> and <strong>Progression</strong> are central teams that most others depend on</li>
                <li>Solid arrows show events/data being sent</li>
                <li>Dashed arrows show dependencies (data being received)</li>
                <li>All systems use <code>player_id</code> for association</li>
            </ul>
        </div>

        <h2>2. Pack Purchase Flow - Detailed DFD</h2>
        <div class="diagram-container">
            <div class="mermaid">
{pack_purchase_dfd}
            </div>
        </div>
        <div class="description">
            <strong>Flow Description:</strong>
            <ol>
                <li>Player initiates pack purchase in CCAS system</li>
                <li>CCAS checks pack cost (coins/gems) from pack configuration</li>
                <li>CCAS requests wallet balance from Economy</li>
                <li>Economy validates sufficient funds</li>
                <li>If funds available: Economy deducts coins/gems, CCAS processes pack purchase</li>
                <li>CCAS rolls for card rarities and selects specific cards</li>
                <li>CCAS checks for duplicates in player collection</li>
                <li>If duplicate: Awards XP/DUST compensation, updates collection</li>
                <li>CCAS emits <code>buy_pack</code> event with transaction details</li>
                <li>Economy listens to event and updates transaction history</li>
                <li>Progression may receive XP updates if cards awarded XP</li>
            </ol>
        </div>

        <h2>3. Coach Hiring Flow</h2>
        <div class="diagram-container">
            <div class="mermaid">
{coach_hiring_dfd}
            </div>
        </div>
        <div class="description">
            <strong>Flow Description:</strong>
            <ol>
                <li>Player selects coach to hire in Coaching system</li>
                <li>Coaching system checks coach cost from configuration</li>
                <li>Coaching requests wallet validation from Economy</li>
                <li>Economy checks if player has sufficient coins</li>
                <li>If sufficient: Economy deducts coins, Coaching assigns coach to team</li>
                <li>Coaching emits <code>hire_coach</code> event</li>
                <li>Economy listens and updates transaction history</li>
                <li>Coaching bonuses may affect Progression XP calculations</li>
            </ol>
        </div>

        <h2>4. Facility Upgrade Flow</h2>
        <div class="diagram-container">
            <div class="mermaid">
{facility_upgrade_dfd}
            </div>
        </div>
        <div class="description">
            <strong>Flow Description:</strong>
            <ol>
                <li>Player initiates facility upgrade in Facilities system</li>
                <li>Facilities checks upgrade cost (coins + gems) based on current level</li>
                <li>Facilities requests wallet validation from Economy</li>
                <li>Economy validates sufficient funds (coins and gems)</li>
                <li>If sufficient: Economy deducts coins/gems, Facilities upgrades facility</li>
                <li>Facilities calculates new multipliers/benefits</li>
                <li>Facilities emits <code>upgrade_facility</code> event</li>
                <li>Economy listens and updates transaction history</li>
                <li>Facility multipliers affect Progression XP gains</li>
            </ol>
        </div>

        <h2>5. Event Synchronization Flow</h2>
        <div class="diagram-container">
            <div class="mermaid">
{event_sync_flow}
            </div>
        </div>
        <div class="description">
            <strong>Event Flow Pattern:</strong>
            <ul>
                <li>Teams emit events when actions occur (pack purchase, coach hire, facility upgrade)</li>
                <li>Economy listens for all spending events to update wallet</li>
                <li>Progression listens for XP-related events</li>
                <li>All events include <code>player_id</code> for association</li>
                <li>Events are processed asynchronously in offline/local storage environment</li>
            </ul>
        </div>

        <div class="note">
            <strong>Note:</strong> Since all systems work offline with local storage and are in separate repos, 
            event synchronization will be implemented through file-based event system or schema placeholders 
            until full integration. Teams are currently documenting their dependencies and event keys.
        </div>
    </div>

    <script>
        mermaid.initialize({{
            startOnLoad: true,
            theme: 'default',
            themeVariables: {{
                primaryColor: '#3498db',
                primaryTextColor: '#2c3e50',
                primaryBorderColor: '#2980b9',
                lineColor: '#34495e',
                secondaryColor: '#ecf0f1',
                tertiaryColor: '#ffffff'
            }},
            flowchart: {{
                useMaxWidth: true,
                htmlLabels: true,
                curve: 'basis'
            }}
        }});
    </script>
</body>
</html>
"""

# System Architecture Diagram
SYSTEM_ARCHITECTURE = """
graph TB
    subgraph "Central Teams"
        Economy[ECONOMY<br/>Wallet Management<br/>Transaction History]
        Progression[PROGRESSION<br/>XP System<br/>Tier Progression]
    end
    
    subgraph "Other Teams"
        CCAS[CCAS/Acquisition<br/>Pack Purchases<br/>Card Collection]
        Coaching[COACHING<br/>Coach Hiring<br/>Team Management]
        Facilities[FACILITIES<br/>Facility Upgrades<br/>Training Multipliers]
    end
    
    CCAS -->|"buy_pack event<br/>(coins, gems)"| Economy
    Coaching -->|"hire_coach event<br/>(coins)"| Economy
    Facilities -->|"upgrade_facility event<br/>(coins, gems)"| Economy
    
    CCAS -.->|"needs wallet balance"| Economy
    Coaching -.->|"needs wallet balance"| Economy
    Facilities -.->|"needs wallet balance"| Economy
    
    CCAS -->|"XP from duplicates"| Progression
    Coaching -.->|"needs XP data"| Progression
    Facilities -->|"XP multipliers"| Progression
    
    Coaching -.->|"needs facility data"| Facilities
    
    style Economy fill:#4A90E2,stroke:#2E5C8A,stroke-width:3px,color:#fff
    style Progression fill:#4A90E2,stroke:#2E5C8A,stroke-width:3px,color:#fff
    style CCAS fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Coaching fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Facilities fill:#7ED321,stroke:#5A9A15,stroke-width:2px
"""

# Pack Purchase DFD
PACK_PURCHASE_DFD = """
flowchart TD
    Start([Player wants to buy pack]) --> CCAS1[CCAS: Get Pack Config]
    CCAS1 --> CCAS2[CCAS: Check Pack Cost<br/>coins: 1000-5000<br/>gems: 0-100]
    CCAS2 --> CheckFunds{Check Wallet<br/>Balance}
    
    CheckFunds -->|Request| Economy1[Economy: Get Wallet Balance]
    Economy1 --> Economy2[Economy: Validate Funds<br/>coins >= cost?<br/>gems >= cost?]
    
    Economy2 -->|Insufficient| Reject([Purchase Rejected<br/>Insufficient Funds])
    Economy2 -->|Sufficient| Economy3[Economy: Deduct Coins/Gems]
    
    Economy3 --> Economy4[Economy: Update Transaction History<br/>type: spend<br/>source: buy_pack]
    Economy4 --> CCAS3[CCAS: Process Pack Purchase]
    
    CCAS3 --> CCAS4[CCAS: Roll for Rarity<br/>common: 80%<br/>uncommon: 15%<br/>rare: 5%<br/>epic: 0%<br/>legendary: 0%]
    CCAS4 --> CCAS5[CCAS: Select Card ID<br/>from Card Catalog]
    CCAS5 --> CheckDupe{Is Duplicate?}
    
    CheckDupe -->|Yes| CCAS6[CCAS: Award Dupe Rewards<br/>XP: dupe_xp<br/>DUST: dupe_dust]
    CheckDupe -->|No| CCAS7[CCAS: Add to Collection]
    CCAS6 --> CCAS7
    
    CCAS7 --> CCAS8[CCAS: Emit buy_pack Event<br/>player_id<br/>pack_id<br/>cost_paid<br/>cards_pulled]
    CCAS8 --> Economy5[Economy: Listen to Event]
    Economy5 --> Economy6[Economy: Update Transaction Log]
    
    CCAS6 --> Progression1[Progression: Receive XP Update]
    Progression1 --> Progression2[Progression: Update Player Tier<br/>if XP threshold met]
    
    CCAS7 --> End([Purchase Complete<br/>Cards Added<br/>Wallet Updated])
    Economy6 --> End
    Progression2 --> End
    
    style Economy1 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy2 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy3 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy4 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy5 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy6 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style CCAS1 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS2 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS3 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS4 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS5 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS6 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS7 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style CCAS8 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Progression1 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Progression2 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
"""

# Coach Hiring DFD
COACH_HIRING_DFD = """
flowchart TD
    Start([Player wants to hire coach]) --> Coaching1[Coaching: Get Coach Info]
    Coaching1 --> Coaching2[Coaching: Check Coach Cost<br/>coins: 500]
    Coaching2 --> CheckFunds{Check Wallet<br/>Balance}
    
    CheckFunds -->|Request| Economy1[Economy: Get Wallet Balance]
    Economy1 --> Economy2[Economy: Validate Funds<br/>coins >= 500?]
    
    Economy2 -->|Insufficient| Reject([Hiring Rejected<br/>Insufficient Funds])
    Economy2 -->|Sufficient| Economy3[Economy: Deduct Coins<br/>amount: 500]
    
    Economy3 --> Economy4[Economy: Update Transaction History<br/>type: spend<br/>source: coach_hiring]
    Economy4 --> Coaching3[Coaching: Assign Coach to Team]
    
    Coaching3 --> Coaching4[Coaching: Apply Coach Bonuses<br/>to team stats]
    Coaching4 --> Coaching5[Coaching: Emit hire_coach Event<br/>player_id<br/>coach_id<br/>cost_paid]
    
    Coaching5 --> Economy5[Economy: Listen to Event]
    Economy5 --> Economy6[Economy: Update Transaction Log]
    
    Coaching4 --> Progression1[Progression: May receive<br/>XP from coaching bonuses]
    Progression1 --> Progression2[Progression: Update if needed]
    
    Coaching3 --> End([Coach Hired<br/>Team Updated<br/>Wallet Deducted])
    Economy6 --> End
    Progression2 --> End
    
    style Economy1 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy2 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy3 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy4 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy5 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy6 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Coaching1 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Coaching2 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Coaching3 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Coaching4 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Coaching5 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Progression1 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Progression2 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
"""

# Facility Upgrade DFD
FACILITY_UPGRADE_DFD = """
flowchart TD
    Start([Player wants to upgrade facility]) --> Facilities1[Facilities: Get Facility Info]
    Facilities1 --> Facilities2[Facilities: Check Upgrade Cost<br/>Level 1→2: 100 coins<br/>Level 2→3: 200 coins<br/>Level 3→4: 400 coins + 1 gem<br/>Level 4→5: 800 coins + 3 gems]
    Facilities2 --> CheckFunds{Check Wallet<br/>Balance}
    
    CheckFunds -->|Request| Economy1[Economy: Get Wallet Balance]
    Economy1 --> Economy2[Economy: Validate Funds<br/>coins >= cost?<br/>gems >= cost?]
    
    Economy2 -->|Insufficient| Reject([Upgrade Rejected<br/>Insufficient Funds])
    Economy2 -->|Sufficient| Economy3[Economy: Deduct Coins/Gems]
    
    Economy3 --> Economy4[Economy: Update Transaction History<br/>type: spend<br/>source: upgrade_facility]
    Economy4 --> Facilities3[Facilities: Upgrade Facility Level]
    
    Facilities3 --> Facilities4[Facilities: Calculate New Multipliers<br/>Level 2: 1.2x<br/>Level 3: 1.5x<br/>etc.]
    Facilities4 --> Facilities5[Facilities: Apply Facility Benefits<br/>to training/XP gains]
    Facilities5 --> Facilities6[Facilities: Emit upgrade_facility Event<br/>player_id<br/>facility_id<br/>new_level<br/>cost_paid]
    
    Facilities6 --> Economy5[Economy: Listen to Event]
    Economy5 --> Economy6[Economy: Update Transaction Log]
    
    Facilities5 --> Progression1[Progression: Receive Facility Multipliers]
    Progression1 --> Progression2[Progression: Apply Multipliers<br/>to XP calculations]
    
    Facilities3 --> End([Facility Upgraded<br/>Multipliers Applied<br/>Wallet Deducted])
    Economy6 --> End
    Progression2 --> End
    
    style Economy1 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy2 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy3 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy4 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy5 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Economy6 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Facilities1 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Facilities2 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Facilities3 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Facilities4 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Facilities5 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Facilities6 fill:#7ED321,stroke:#5A9A15,stroke-width:2px
    style Progression1 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
    style Progression2 fill:#4A90E2,stroke:#2E5C8A,stroke-width:2px,color:#fff
"""

# Event Synchronization Flow
EVENT_SYNC_FLOW = """
sequenceDiagram
    participant Player
    participant CCAS as CCAS/Acquisition
    participant Coaching as Coaching
    participant Facilities as Facilities
    participant Economy as Economy
    participant Progression as Progression
    
    Note over Player,Economy: Pack Purchase Flow
    Player->>CCAS: Request pack purchase
    CCAS->>Economy: Check wallet balance
    Economy-->>CCAS: Balance: coins=2000, gems=50
    CCAS->>CCAS: Validate affordability
    CCAS->>Economy: Deduct coins/gems
    Economy->>Economy: Update wallet
    CCAS->>CCAS: Process pack, roll cards
    CCAS->>CCAS: Check duplicates, award XP/DUST
    CCAS->>Economy: Emit buy_pack event
    Economy->>Economy: Update transaction history
    CCAS->>Progression: Emit XP update (if dupe reward)
    Progression->>Progression: Update player tier if needed
    
    Note over Player,Economy: Coach Hiring Flow
    Player->>Coaching: Request coach hire
    Coaching->>Economy: Check wallet balance
    Economy-->>Coaching: Balance: coins=1500
    Coaching->>Economy: Deduct coins (500)
    Economy->>Economy: Update wallet
    Coaching->>Coaching: Assign coach to team
    Coaching->>Economy: Emit hire_coach event
    Economy->>Economy: Update transaction history
    
    Note over Player,Economy: Facility Upgrade Flow
    Player->>Facilities: Request facility upgrade
    Facilities->>Economy: Check wallet balance
    Economy-->>Facilities: Balance: coins=500, gems=2
    Facilities->>Economy: Deduct coins/gems
    Economy->>Economy: Update wallet
    Facilities->>Facilities: Upgrade facility, calculate multipliers
    Facilities->>Economy: Emit upgrade_facility event
    Economy->>Economy: Update transaction history
    Facilities->>Progression: Apply facility multipliers to XP
    Progression->>Progression: Recalculate XP gains
"""

def generate_html():
    """Generate the HTML file with all diagrams"""
    html_content = HTML_TEMPLATE.format(
        system_architecture=SYSTEM_ARCHITECTURE,
        pack_purchase_dfd=PACK_PURCHASE_DFD,
        coach_hiring_dfd=COACH_HIRING_DFD,
        facility_upgrade_dfd=FACILITY_UPGRADE_DFD,
        event_sync_flow=EVENT_SYNC_FLOW
    )
    
    output_path = "system_diagrams.html"
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(html_content)
    
    print(f"✅ Diagrams generated successfully!")
    print(f"📄 Open '{output_path}' in your browser to view the interactive diagrams")
    print(f"🌐 File location: {output_path}")

if __name__ == "__main__":
    generate_html()


