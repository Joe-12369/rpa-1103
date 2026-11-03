# -*- coding: utf-8 -*-

import os
from pathlib import Path

import yaml

from rpa_sign_client import run_sign_batches


CONFIG_FILE = "sign.yaml"

target_path = os.path.abspath(".")
print("Target path at {}".format(target_path))

config_path = os.path.abspath(os.path.join(target_path, CONFIG_FILE))
print("Config file at {}".format(config_path))


def list_glob(patterns, target_dir):
    file_list = [
        [str(file_path.resolve().absolute()) for file_path in target_dir.glob(pattern)]
        for pattern in patterns
    ]
    return [file_path for files in file_list for file_path in files]


def process():
    sign_code = (os.environ.get("SIGN_CODE") or "").strip()
    if not sign_code:
        print("Skip signing because SIGN_CODE is empty")
        return

    node_executable = (os.environ.get("RPA_SIGN_CLI") or "").strip()
    cli_script = (os.environ.get("RPA_SIGN_CLI_SCRIPT") or "").strip()
    if not node_executable or not cli_script:
        raise RuntimeError("RPA_SIGN_CLI and RPA_SIGN_CLI_SCRIPT are required")

    with open(config_path, "r", encoding="utf8") as config_file:
        content = yaml.safe_load(config_file)
        target_files = content["files"] or []
        target_glob = content["glob"] or []
        exclude_glob = content["exclude_glob"] or []

    explicit_files = {
        os.path.abspath(os.path.join(target_path, target_file))
        for target_file in target_files
    }
    target_dir = Path(target_path)
    glob_files = set(list_glob(target_glob, target_dir))
    exclude_files = set(list_glob(exclude_glob, target_dir))
    targets = sorted((glob_files - exclude_files) | explicit_files, key=os.path.normcase)

    print("targets:")
    for target in targets:
        print(target)

    exit_code = run_sign_batches(
        node_executable=node_executable,
        cli_script=cli_script,
        files=targets,
        artifact_kind="ii-rpa-binaries",
        pipeline_id=os.environ.get("CI_PIPELINE_ID") or os.environ.get("BUILD_NUMBER") or "manual",
    )
    if exit_code != 0:
        raise RuntimeError("Centralized signing failed with exit code {}".format(exit_code))


if __name__ == "__main__":
    process()
