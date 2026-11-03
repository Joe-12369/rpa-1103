# -*- coding: utf-8 -*-

import os
import sys
import uuid
import yaml
import shutil
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

TMP_DIR = r'.\.virbox_tmp'
# 从环境变量读取工具路径
VIRBOX_PATH = os.environ.get('VIRBOX3_EXE', r'virboxprotector_con')
COMMANDS = {
	'reactor': 'dotNET_Reactor -file {0} -targetfile {1}',
	'virbox': f'"{VIRBOX_PATH}" {{0}} -o {{1}} --rename=0'
}
protect_tool = next(iter(sys.argv[1:]), None) or 'reactor'
print('Use {} as protect tool.'.format(protect_tool))
command_str = COMMANDS[protect_tool]

def protect_file(target):
	target_tmp = [os.path.abspath(target), os.path.abspath(os.path.join(TMP_DIR, uuid.uuid4().hex))]
	print('Process file {}.'.format(target_tmp[0]))
	ret = os.system(command_str.format(target_tmp[0], target_tmp[1]))
	if ret != 0:
		raise Exception('Virbox protector execute failed when handling {}.'.format(target_tmp[0]))
	return target_tmp

current_path = Path('.')
def list_glob(glob):
	file_list = [[str(fp.resolve().absolute()) for fp in current_path.glob(tg)] for tg in glob]
	return [f for fs in file_list for f in fs]

try:
	with open(r'.\.protect.yml', 'r', encoding='utf8') as file:
		content = yaml.safe_load(file)
		target_files = content['files'] or []
		target_glob = content['glob'] or []
		exclude_glob = content['exclude_glob'] or []

	target_files = set([os.path.abspath(tf) for tf in target_files])
	glob_files = set(list_glob(target_glob))
	exclude_files = set(list_glob(exclude_glob))

	print('files:')
	[print(f) for f in target_files]
	print('glob_files:')
	[print(f) for f in glob_files]
	print('exclude_glob:')
	[print(f) for f in exclude_files]

	merge_glob = (glob_files - exclude_files) | target_files
	print('targets:')
	[print(f) for f in merge_glob]

	caches = []
	with ThreadPoolExecutor(1) as executor:
		results = executor.map(protect_file, merge_glob)
		[caches.append(r) for r in results]
	for cache in caches:
		if os.path.exists(cache[1]):
			print('Overwrite {}.'.format(cache[0]))
			shutil.move(cache[1], cache[0])
finally:
	shutil.rmtree(TMP_DIR, ignore_errors=True)