import bpy
import json
import os
import sys

args = sys.argv[sys.argv.index('--') + 1:]
output_dir, report_path = args[0], args[1]
armatures = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']
if len(armatures) != 1:
    raise RuntimeError('Expected one armature')
armature = armatures[0]
meshes = sorted([obj for obj in bpy.data.objects if obj.type == 'MESH' and len(obj.vertex_groups) > 0], key=lambda obj: obj.name)
os.makedirs(output_dir, exist_ok=True)
bpy.context.scene.layers = [True] * 20
parts = []
for obj in meshes:
    modifiers = [modifier for modifier in obj.modifiers if modifier.type == 'ARMATURE']
    if not modifiers:
        modifier = obj.modifiers.new(name='CanonicalArmature', type='ARMATURE')
        modifier.object = armature
    else:
        for modifier in modifiers:
            modifier.object = armature
    obj.hide = False
    obj.hide_render = False
    bpy.ops.object.select_all(action='DESELECT')
    armature.select = True
    obj.select = True
    bpy.context.scene.objects.active = armature
    path = os.path.join(output_dir, obj.name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True,
        object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=True,
        add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y')
    parts.append({'name': obj.name, 'file': os.path.basename(path),
        'vertices': len(obj.data.vertices),
        'triangles': sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons)})
with open(report_path, 'w') as handle:
    json.dump({'source': bpy.data.filepath, 'armature': armature.name,
        'bone_count': len(armature.data.bones), 'parts': parts}, handle, indent=2, sort_keys=True)
