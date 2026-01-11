<?php
//host sftp://citmalumnes.upc.es
//hugocc2
//Vf2hAqU5nxL6

// Configuracion
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, GET, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

// Si es OPTIONS, terminar
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    exit(0);
}

// Conexion a la database
$servername = "localhost";
$username = "hugocc2";
$password = "Vf2hAqU5nxL6";
$database = "hugocc2";

$conn = new mysqli($servername, $username, $password, $database);

if ($conn->connect_error) {
    die(json_encode(["success" => false, "error" => "Database connection failed: " . $conn->connect_error]));
}

// Obtener datos POST
$input = $_POST;

// ==============================================
// ENDPOINT: Registrar muerte del jugador
// ==============================================
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($input['metric_type']) && $input['metric_type'] === 'player_death') {
    
    // el PHP espera campos directos, pero Unity envia "data" dentro
    // Necesitamos adaptar la estructura
    $data = $input['data'] ?? $input; // Compatible con ambas estructuras
    
    $required_fields = ['player_id', 'death_cause', 'position_x', 'position_y', 'position_z'];
    $missing_fields = [];
    
    foreach ($required_fields as $field) {
        if (!isset($data[$field])) {
            $missing_fields[] = $field;
        }
    }
    
    if (!empty($missing_fields)) {
        echo json_encode(["success" => false, "error" => "Missing fields: " . implode(', ', $missing_fields)]);
        exit;
    }
    
    $player_id = $conn->real_escape_string($data['player_id']);
    $death_cause = $conn->real_escape_string($data['death_cause']);
    $pos_x = floatval($data['position_x']);
    $pos_y = floatval($data['position_y']);
    $pos_z = floatval($data['position_z']);
    $zone_name = isset($data['zone_name']) ? $conn->real_escape_string($data['zone_name']) : 'unknown';
    $lake_name = isset($data['lake_name']) ? $conn->real_escape_string($data['lake_name']) : null;
    $level_name = isset($data['level_name']) ? $conn->real_escape_string($data['level_name']) : 'unknown';
    
    // Insertar muerte (sin lake_name , lo anadimos despues)
    $stmt = $conn->prepare("
        INSERT INTO player_deaths 
        (player_id, death_cause, position_x, position_y, position_z, zone_name, level_name) 
        VALUES (?, ?, ?, ?, ?, ?, ?)
    ");
    
    $stmt->bind_param("ssdddss", $player_id, $death_cause, $pos_x, $pos_y, $pos_z, $zone_name, $level_name);
    
    if ($stmt->execute()) {
        $death_id = $stmt->insert_id;
        
        // Si es muerte por acido y tenemos nombre de lago
        if ($death_cause === 'acido' && $lake_name) {
            // Actualizar tabla acid_lakes si existe
            updateAcidLakeDeaths($conn, $lake_name, $pos_x, $pos_y, $pos_z, $zone_name);
            
            // tmb actualizar player_deaths con lake_name
            $update_stmt = $conn->prepare("UPDATE player_deaths SET lake_name = ? WHERE id = ?");
            $update_stmt->bind_param("si", $lake_name, $death_id);
            $update_stmt->execute();
            $update_stmt->close();
        }
        
        echo json_encode([
            "success" => true,
            "message" => "Death recorded",
            "death_id" => $death_id
        ]);
    } else {
        echo json_encode(["success" => false, "error" => "Failed to record death: " . $stmt->error]);
    }
    
    $stmt->close();
}

// ==============================================
// ENDPOINT: Registrar cubo destruido
// ==============================================
elseif ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($input['metric_type']) && $input['metric_type'] === 'cube_destroyed') {
    
    $data = $input['data'] ?? $input;
    
    $required_fields = ['cube_type', 'position_x', 'position_y', 'position_z'];
    $missing_fields = [];
    
    foreach ($required_fields as $field) {
        if (!isset($data[$field])) {
            $missing_fields[] = $field;
        }
    }
    
    if (!empty($missing_fields)) {
        echo json_encode(["success" => false, "error" => "Missing fields: " . implode(', ', $missing_fields)]);
        exit;
    }
    
    $cube_type = $conn->real_escape_string($data['cube_type']);
    $pos_x = floatval($data['position_x']);
    $pos_y = floatval($data['position_y']);
    $pos_z = floatval($data['position_z']);
    $zone_name = isset($data['zone_name']) ? $conn->real_escape_string($data['zone_name']) : 'unknown';
    
    // Version simplificada: siempre insertar nuevo registro
    $stmt = $conn->prepare("
        INSERT INTO destructible_cubes 
        (cube_type, position_x, position_y, position_z, zone_name) 
        VALUES (?, ?, ?, ?, ?)
    ");
    
    $stmt->bind_param("sddds", $cube_type, $pos_x, $pos_y, $pos_z, $zone_name);
    
    if ($stmt->execute()) {
        echo json_encode([
            "success" => true,
            "message" => "Cube destruction recorded",
            "cube_id" => $stmt->insert_id
        ]);
    } else {
        echo json_encode(["success" => false, "error" => "Failed to record cube destruction: " . $stmt->error]);
    }
    
    $stmt->close();
}

// ====================================
// ENDPOINT: Registrar enemigo matado
// ====================================
elseif ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($input['metric_type']) && $input['metric_type'] === 'enemy_killed') {
    
    $data = $input['data'] ?? $input;
    
    $required_fields = ['enemy_type', 'time_to_kill'];
    $missing_fields = [];
    
    foreach ($required_fields as $field) {
        if (!isset($data[$field])) {
            $missing_fields[] = $field;
        }
    }
    
    if (!empty($missing_fields)) {
        echo json_encode(["success" => false, "error" => "Missing fields: " . implode(', ', $missing_fields)]);
        exit;
    }
    
    $enemy_type = $conn->real_escape_string($data['enemy_type']);
    $time_to_kill = floatval($data['time_to_kill']);
    $damage_dealt = isset($data['damage_dealt']) ? floatval($data['damage_dealt']) : 0;
    
    // Version simplificada: siempre insertar nuevo registro
    $stmt = $conn->prepare("
        INSERT INTO enemy_stats 
        (enemy_type, time_to_kill, damage_dealt) 
        VALUES (?, ?, ?)
    ");
    
    $stmt->bind_param("sdd", $enemy_type, $time_to_kill, $damage_dealt);
    
    if ($stmt->execute()) {
        echo json_encode([
            "success" => true,
            "message" => "Enemy kill recorded",
            "enemy_id" => $stmt->insert_id
        ]);
    } else {
        echo json_encode(["success" => false, "error" => "Failed to record enemy kill: " . $stmt->error]);
    }
    
    $stmt->close();
}

// ==============================
// ENDPOINTS GET PARA ANALISIS
// ===============================
elseif ($_SERVER['REQUEST_METHOD'] === 'GET') {
    
    // Endpoint: Puntos con mas muertes (heatmap)
    if (isset($_GET['get_death_points'])) {
        $limit = isset($_GET['limit']) ? intval($_GET['limit']) : 50;
        $zone = isset($_GET['zone']) ? $conn->real_escape_string($_GET['zone']) : null;
        
        $query = "
            SELECT 
                ROUND(position_x, 1) as grid_x,
                ROUND(position_z, 1) as grid_z,
                COUNT(*) as death_count,
                GROUP_CONCAT(DISTINCT death_cause) as causes,
                zone_name
            FROM player_deaths
            WHERE 1=1
        ";
        
        if ($zone && $zone !== 'all') {
            $query .= " AND zone_name = '$zone' ";
        }
        
        $query .= "
            GROUP BY grid_x, grid_z, zone_name
            ORDER BY death_count DESC
            LIMIT $limit
        ";
        
        $result = $conn->query($query);
        
        if (!$result) {
            echo json_encode(["success" => false, "error" => "Query failed: " . $conn->error]);
            exit;
        }
        
        $data = [];
        while ($row = $result->fetch_assoc()) {
            $data[] = $row;
        }
        
        echo json_encode([
            "success" => true,
            "total_points" => count($data),
            "death_points" => $data
        ]);
    }
    
    // Endpoint: Muertes por causa
    elseif (isset($_GET['get_death_causes'])) {
        $query = "
            SELECT 
                death_cause,
                COUNT(*) as count,
                ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM player_deaths), 2) as percentage
            FROM player_deaths
            GROUP BY death_cause
            ORDER BY count DESC
        ";
        
        $result = $conn->query($query);
        $data = [];
        while ($row = $result->fetch_assoc()) {
            $data[] = $row;
        }
        
        echo json_encode([
            "success" => true,
            "death_causes" => $data
        ]);
    }
    
    // Endpoint: Cubos destruidos
    elseif (isset($_GET['get_cube_positions'])) {
        $limit = isset($_GET['limit']) ? intval($_GET['limit']) : 100;
        
        $query = "
            SELECT 
                ROUND(position_x, 1) as grid_x,
                ROUND(position_z, 1) as grid_z,
                COUNT(*) as destruction_count,
                cube_type
            FROM destructible_cubes
            GROUP BY grid_x, grid_z, cube_type
            ORDER BY destruction_count DESC
            LIMIT $limit
        ";
        
        $result = $conn->query($query);
        
        if (!$result) {
            echo json_encode(["success" => false, "error" => "Query failed: " . $conn->error]);
            exit;
        }
        
        $data = [];
        while ($row = $result->fetch_assoc()) {
            $data[] = $row;
        }
        
        echo json_encode([
            "success" => true,
            "cube_positions" => $data
        ]);
    }
        
        // Total general
        $query_total = "SELECT COUNT(*) as total_cubes, SUM(destruction_count) as total_destructions FROM destructible_cubes";
        $result_total = $conn->query($query_total);
        $total = $result_total->fetch_assoc();
        
        echo json_encode([
            "success" => true,
            "total_cubes" => $total['total_cubes'] ?? 0,
            "total_destructions" => $total['total_destructions'] ?? 0,
            "cube_type_stats" => $cube_stats
        ]);
    }
    
    // Endpoint: Enemigos
    elseif (isset($_GET['get_enemy_stats'])) {
        // Enemigos por tipo
        $query = "
            SELECT 
                enemy_type,
                COUNT(*) as times_killed,
                AVG(time_to_kill) as avg_time_to_kill,
                SUM(damage_dealt) as total_damage
            FROM enemy_stats
            GROUP BY enemy_type
            ORDER BY times_killed DESC
        ";
        
        $result = $conn->query($query);
        $enemy_stats = [];
        while ($row = $result->fetch_assoc()) {
            $enemy_stats[] = $row;
        }
        
        echo json_encode([
            "success" => true,
            "enemy_stats" => $enemy_stats
        ]);
    }
    
    // Endpoint: Muertes en lagos de acido
    elseif (isset($_GET['get_acid_lake_deaths'])) {
        // Primero verificar si la tabla existe
        $table_check = $conn->query("SHOW TABLES LIKE 'acid_lakes'");
        
        if ($table_check->num_rows > 0) {
            $query = "
                SELECT 
                    lake_name,
                    position_x,
                    position_z,
                    deaths_count,
                    zone_name,
                    last_updated
                FROM acid_lakes
                ORDER BY deaths_count DESC
            ";
            
            $result = $conn->query($query);
            $lakes = [];
            while ($row = $result->fetch_assoc()) {
                $lakes[] = $row;
            }
            
            echo json_encode([
                "success" => true,
                "acid_lakes" => $lakes
            ]);
        } else {
            // Si no existe la tabla, usar player_deaths
            $query = "
                SELECT 
                    lake_name,
                    COUNT(*) as deaths_count,
                    zone_name
                FROM player_deaths
                WHERE death_cause = 'acido' AND lake_name IS NOT NULL
                GROUP BY lake_name, zone_name
                ORDER BY deaths_count DESC
            ";
            
            $result = $conn->query($query);
            $lakes = [];
            while ($row = $result->fetch_assoc()) {
                $lakes[] = $row;
            }
            
            echo json_encode([
                "success" => true,
                "acid_lakes" => $lakes,
                "note" => "Data from player_deaths table"
            ]);
        }
    }
    
    // Endpoint: Dashboard simple
    elseif (isset($_GET['get_dashboard'])) {
        $dashboard = [];
        
        // Muertes totales
        $result = $conn->query("SELECT COUNT(*) as total FROM player_deaths");
        $dashboard['total_deaths'] = $result->fetch_assoc()['total'] ?? 0;
        
        // Cubos destruidos
        $result = $conn->query("SELECT COUNT(*) as total FROM destructible_cubes");
        $dashboard['total_cubes'] = $result->fetch_assoc()['total'] ?? 0;
        
        // Enemigos eliminados
        $result = $conn->query("SELECT COUNT(*) as total FROM enemy_stats");
        $dashboard['total_enemies_killed'] = $result->fetch_assoc()['total'] ?? 0;
        
        // ultimas muertes
        $result = $conn->query("
            SELECT death_cause, zone_name, timestamp 
            FROM player_deaths 
            ORDER BY timestamp DESC 
            LIMIT 10
        ");
        $dashboard['recent_deaths'] = [];
        while ($row = $result->fetch_assoc()) {
            $dashboard['recent_deaths'][] = $row;
        }
        
        echo json_encode([
            "success" => true,
            "dashboard" => $dashboard,
            "timestamp" => date('Y-m-d H:i:s')
        ]);
    }
    
    // Endpoint: Health check
    else {
        echo json_encode([
            "success" => true,
            "message" => "Game Metrics API is running",
            "timestamp" => date('Y-m-d H:i:s'),
            "endpoints" => [
                "POST" => ["player_death", "cube_destroyed", "enemy_killed"],
                "GET" => [
                    "get_death_points",
                    "get_death_causes", 
                    "get_cube_stats",
                    "get_enemy_stats",
                    "get_acid_lake_deaths",
                    "get_dashboard"
                ]
            ]
        ]);
    }
}

// =======================
// FUNCIONES AUXILIARES
// =======================

function updateAcidLakeDeaths($conn, $lake_name, $pos_x, $pos_y, $pos_z, $zone_name) {
    $lake_name = $conn->real_escape_string($lake_name);
    $zone_name = $conn->real_escape_string($zone_name);
    
    // Primero verificar si la tabla existe
    $table_check = $conn->query("SHOW TABLES LIKE 'acid_lakes'");
    
    if ($table_check->num_rows > 0) {
        $query = "
            INSERT INTO acid_lakes (lake_name, position_x, position_y, position_z, deaths_count, zone_name) 
            VALUES ('$lake_name', $pos_x, $pos_y, $pos_z, 1, '$zone_name')
            ON DUPLICATE KEY UPDATE deaths_count = deaths_count + 1, last_updated = CURRENT_TIMESTAMP
        ";
        $conn->query($query);
    }
}

// Cerrar conexion
$conn->close();
?>