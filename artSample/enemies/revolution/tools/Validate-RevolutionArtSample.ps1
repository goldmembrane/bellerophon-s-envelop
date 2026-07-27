$ErrorActionPreference = 'Stop'

$sampleRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = (Resolve-Path -LiteralPath (Join-Path $sampleRoot '..\..\..')).Path
$sourcePath = Join-Path $workspaceRoot 'Assets\_Project\Art\Enemies\Revolution\Models\Revolution.fbx'

$expectedModelHash = '645226EEFA4AEBE8CF43168B8A16E0595506E77F032ADBABFB394DB67FFA578E'
$expectedReferenceHash = 'C7FC6DD7052ABD72E478AC2F84A2616203D82B91153053BEAF61B598F2BB73DC'

$externalCandidates = @(
    Get-ChildItem -LiteralPath (Join-Path $workspaceRoot 'enemies model') -File -Filter '*volution.fbx' |
        Where-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash -eq $expectedModelHash }
)
$referenceCandidates = @(
    Get-ChildItem -LiteralPath (Join-Path $workspaceRoot 'image') -File -Filter '*volution-attack.png' |
        Where-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash -eq $expectedReferenceHash }
)
if ($externalCandidates.Count -ne 1 -or $referenceCandidates.Count -ne 1) {
    throw 'Expected exactly one hash-matched source model and reference image.'
}
$externalPath = $externalCandidates[0].FullName
$referencePath = $referenceCandidates[0].FullName

$sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$externalHash = (Get-FileHash -LiteralPath $externalPath -Algorithm SHA256).Hash
$referenceHash = (Get-FileHash -LiteralPath $referencePath -Algorithm SHA256).Hash
if ($sourceHash -ne $expectedModelHash -or $externalHash -ne $expectedModelHash) {
    throw 'Project/source Revolution FBX hash mismatch.'
}
if ($referenceHash -ne $expectedReferenceHash) {
    throw 'Reference image hash mismatch.'
}

$geometry = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'GEOMETRY_PRESERVATION.json') | ConvertFrom-Json
if (
    $geometry.geometry_changed -or
    -not $geometry.vertex_coordinates_exact_match -or
    -not $geometry.polygon_topology_exact_match -or
    -not $geometry.uv_data_exact_match -or
    -not $geometry.armature_exact_match -or
    $geometry.before.vertices -ne 2307 -or
    $geometry.before.polygons -ne 3945 -or
    $geometry.before.loops -ne 11835 -or
    $geometry.before.bones -ne 24
) {
    throw 'Geometry preservation report failed.'
}

$armSymmetry = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'ARM_COLOR_SYMMETRY.json') | ConvertFrom-Json
$armCopy = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'SCREEN_RIGHT_ARM_COPY.json') | ConvertFrom-Json
if (
    -not $armSymmetry.right_to_left_copy_applied -or
    -not $armSymmetry.mirrored_texture_x_coordinate -or
    $armSymmetry.default_preserved_arm_polygons -ne 0 -or
    $armSymmetry.source_view_side -ne 'screen right' -or
    $armCopy.source_view_side -ne 'screen right' -or
    $armCopy.total_changed_polygons -ne 281 -or
    -not $armCopy.all_records_valid
) {
    throw 'Screen-right arm copy report failed.'
}

$torso = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'TORSO_MATERIAL_ASSIGNMENT.json') | ConvertFrom-Json
if (
    $torso.primary_outer_shell.component_id -ne 0 -or
    $torso.primary_outer_shell.polygon_count -ne 553 -or
    $torso.primary_outer_shell.material -ne 'body_panel' -or
    $torso.primary_outer_shell.material_distribution.body_panel -ne 553 -or
    $torso.primary_outer_shell.base_texture -ne 'textures/reference_body_panel_direct_crop.png' -or
    $torso.primary_outer_shell.wear_detail_texture -ne 'textures/reference_body_wear_direct_crop.png' -or
    $torso.primary_outer_shell.color_mix_factor -ne 0.35 -or
    -not $torso.primary_outer_shell.roughness_from_wear_luminance -or
    -not $torso.primary_outer_shell.bump_from_wear_luminance -or
    $torso.inset_steel.component_id -ne 1 -or
    $torso.inset_steel.polygon_count -ne 114 -or
    $torso.inset_steel.material -ne 'torso_inset_steel' -or
    $torso.inset_steel.material_distribution.torso_inset_steel -ne 114 -or
    $torso.inset_steel.base_texture -ne 'textures/reference_body_wear_direct_crop.png' -or
    $torso.torso_surface_polygon_count -ne 667 -or
    $torso.unpainted_torso_polygons -ne 0 -or
    -not $torso.all_torso_sides_reference_painted -or
    $torso.mesh_changed -or
    $torso.uv_changed -or
    $torso.arm_copy_total_changed_polygons -ne $armCopy.total_changed_polygons -or
    -not $torso.arm_copy_all_records_valid
) {
    throw 'Torso material assignment report failed.'
}

$shoulderJoint = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'SHOULDER_JOINT_MATERIAL_ASSIGNMENT.json') | ConvertFrom-Json
$expectedShoulderComponents = @(2, 3, 14, 16, 25, 27, 40, 55)
$actualShoulderComponents = @($shoulderJoint.components | ForEach-Object { [int]$_.component_id } | Sort-Object)
if (
    $shoulderJoint.shoulder_joint_polygon_count -ne 310 -or
    $shoulderJoint.connector_frame.changed_polygon_count -ne 97 -or
    $shoulderJoint.shoulder_connection_polygon_count -ne 407 -or
    $shoulderJoint.unpainted_shoulder_joint_polygons -ne 0 -or
    $shoulderJoint.unpainted_shoulder_connection_polygons -ne 0 -or
    $shoulderJoint.changed_polygons -ne 407 -or
    $shoulderJoint.non_target_changed_polygons -ne 0 -or
    $shoulderJoint.torso_surface_polygon_count -ne 667 -or
    -not $shoulderJoint.torso_materials_unchanged -or
    $shoulderJoint.mesh_changed -or
    $shoulderJoint.uv_changed -or
    $shoulderJoint.arm_copy_total_changed_polygons -ne 281 -or
    -not $shoulderJoint.arm_copy_all_records_valid -or
    (Compare-Object $expectedShoulderComponents $actualShoulderComponents)
) {
    throw 'Shoulder joint material assignment report failed.'
}
foreach ($component in $shoulderJoint.components) {
    $expectedMaterial = if ($component.component_id -in 14, 16, 25, 27) {
        'body_panel'
    } else {
        'dark_mechanics'
    }
    if (
        $component.material -ne $expectedMaterial -or
        $component.material_distribution.$expectedMaterial -ne $component.polygon_count
    ) {
        throw "Shoulder joint component material failed: $($component.component_id)"
    }
}

$cropReport = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'CROP_PROVENANCE.json') | ConvertFrom-Json
if ($cropReport.reference_sha256 -ne $expectedReferenceHash -or $cropReport.regions.Count -ne 8) {
    throw 'Reference crop provenance failed.'
}
foreach ($region in $cropReport.regions) {
    $cropPath = Join-Path $sampleRoot ($region.file -replace '/', '\')
    if (-not (Test-Path -LiteralPath $cropPath -PathType Leaf)) {
        throw "Missing direct crop: $($region.file)"
    }
    if ((Get-FileHash -LiteralPath $cropPath -Algorithm SHA256).Hash -ne $region.sha256) {
        throw "Direct crop hash mismatch: $($region.file)"
    }
}

$exportInspection = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'EXPORT_INSPECTION.json') | ConvertFrom-Json
foreach ($kind in 'blend', 'fbx') {
    $item = $exportInspection.$kind
    if (
        $item.meshes -ne 1 -or
        $item.vertices -ne 2307 -or
        $item.polygons -ne 3945 -or
        $item.loops -ne 11835 -or
        $item.armatures -ne 1 -or
        $item.bones -ne 24
    ) {
        throw "Export inspection failed: $kind"
    }
}
if (
    $exportInspection.glb.meshes -ne 1 -or
    $exportInspection.glb.polygons -ne 3945 -or
    $exportInspection.glb.loops -ne 11835
) {
    throw 'Static GLB inspection failed.'
}

$required = @(
    'index.html',
    'README.md',
    'TEXTURE_ANALYSIS.md',
    'APPROVAL_STATUS.json',
    'ASSET_MANIFEST.json',
    'ARM_COLOR_SYMMETRY.json',
    'SCREEN_RIGHT_ARM_COPY.json',
    'TORSO_MATERIAL_ASSIGNMENT.json',
    'SHOULDER_JOINT_MATERIAL_ASSIGNMENT.json',
    'renders\02_front_reference_material.png',
    'renders\03_three_quarter_reference_material.png',
    'renders\04_side_reference_material.png',
    'renders\05_rear_preserved.png',
    'renders\06_before_after_same_mesh.png',
    'renders\07_reference_and_sample.png',
    'exports\revolution_replaced_model_reference_sample.blend',
    'exports\revolution_replaced_model_reference_sample.fbx',
    'exports\revolution_replaced_model_reference_sample.glb'
)
foreach ($relativePath in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $sampleRoot $relativePath) -PathType Leaf)) {
        throw "Missing art sample file: $relativePath"
    }
}

$html = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sampleRoot 'index.html')
$imageReferences = [regex]::Matches($html, '<img\s+[^>]*src="([^"]+)"')
foreach ($match in $imageReferences) {
    $relativePath = [System.Uri]::UnescapeDataString($match.Groups[1].Value) -replace '/', '\'
    if (-not (Test-Path -LiteralPath (Join-Path $sampleRoot $relativePath) -PathType Leaf)) {
        throw "Broken HTML image reference: $relativePath"
    }
}

$prohibited = Get-ChildItem -LiteralPath (Join-Path $sampleRoot 'textures') -File |
    Where-Object { $_.Name -match '(normal|roughness|metallic|height|emission)' }
if ($prohibited) {
    throw 'Generated or inferred PBR texture file detected.'
}

[pscustomobject]@{
    Result = 'PASS'
    SourceAndProjectHashMatch = $true
    ReferenceHashMatch = $true
    GeometryUnchanged = $true
    Vertices = 2307
    Polygons = 3945
    Loops = 11835
    Bones = 24
    DirectReferenceCrops = 8
    TorsoTexturedMaterial = $true
    TorsoPrimaryPolygons = $torso.primary_outer_shell.polygon_count
    TorsoInsetPolygons = $torso.inset_steel.polygon_count
    TorsoSurfacePolygons = $torso.torso_surface_polygon_count
    UnpaintedTorsoPolygons = $torso.unpainted_torso_polygons
    ShoulderJointPolygons = $shoulderJoint.shoulder_joint_polygon_count
    UnpaintedShoulderJointPolygons = $shoulderJoint.unpainted_shoulder_joint_polygons
    ShoulderConnectorFramePolygons = $shoulderJoint.connector_frame.changed_polygon_count
    ShoulderConnectionPolygons = $shoulderJoint.shoulder_connection_polygon_count
    UnpaintedShoulderConnectionPolygons = $shoulderJoint.unpainted_shoulder_connection_polygons
    ArmColorSymmetry = $true
    ScreenRightBasis = $true
    CopiedArmPolygons = $armCopy.total_changed_polygons
    DefaultPreservedArmPolygons = 0
    HtmlImageReferences = $imageReferences.Count
    UnityAppearanceApplied = $false
} | ConvertTo-Json
