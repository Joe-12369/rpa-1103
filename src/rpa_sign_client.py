import hashlib
import os
import subprocess


DEFAULT_MAX_FILES = 256
DEFAULT_MAX_COMMAND_CHARS = 28000
FATAL_EXIT_CODES = {10, 16, 17, 18, 20, 21}


def normalize_files(files):
    result = []
    seen = set()
    for file_path in files:
        normalized = os.path.abspath(os.path.normpath(str(file_path)))
        key = os.path.normcase(normalized)
        if key in seen:
            continue
        seen.add(key)
        result.append(normalized)
    result.sort(key=lambda item: os.path.normcase(item))
    return result


def make_batch_id(pipeline_id, artifact_kind, batch_index, files):
    digest = hashlib.sha256("\n".join(files).encode("utf-8")).hexdigest()[:16]
    return "{}:{}:chunk-{:04d}:{}".format(
        pipeline_id,
        artifact_kind,
        batch_index,
        digest,
    )


def build_command(node_executable, cli_script, files, artifact_kind, batch_id):
    return [node_executable, cli_script, "/file"] + list(files) + [
        "/kind",
        artifact_kind,
        "/request-id",
        batch_id,
    ]


def split_file_batches(
    node_executable,
    cli_script,
    files,
    artifact_kind,
    pipeline_id,
    max_files=DEFAULT_MAX_FILES,
    max_chars=DEFAULT_MAX_COMMAND_CHARS,
):
    normalized_files = normalize_files(files)
    batches = []
    current = []
    for file_path in normalized_files:
        candidate = current + [file_path]
        batch_id = make_batch_id(pipeline_id, artifact_kind, len(batches) + 1, candidate)
        command = build_command(
            node_executable,
            cli_script,
            candidate,
            artifact_kind,
            batch_id,
        )
        if current and (
            len(candidate) > max_files
            or len(subprocess.list2cmdline(command)) > max_chars
        ):
            batches.append(current)
            current = [file_path]
            single_id = make_batch_id(
                pipeline_id,
                artifact_kind,
                len(batches) + 1,
                current,
            )
            single_command = build_command(
                node_executable,
                cli_script,
                current,
                artifact_kind,
                single_id,
            )
            if len(subprocess.list2cmdline(single_command)) > max_chars:
                raise ValueError("A single signing path exceeds the Windows command-line limit")
        else:
            current = candidate
    if current:
        batches.append(current)
    return batches


def run_sign_batches(
    node_executable,
    cli_script,
    files,
    artifact_kind,
    pipeline_id,
    max_files=DEFAULT_MAX_FILES,
    max_chars=DEFAULT_MAX_COMMAND_CHARS,
    run_command=subprocess.run,
):
    batches = split_file_batches(
        node_executable,
        cli_script,
        files,
        artifact_kind,
        pipeline_id,
        max_files=max_files,
        max_chars=max_chars,
    )
    first_failure = 0
    for index, batch in enumerate(batches, start=1):
        batch_id = make_batch_id(pipeline_id, artifact_kind, index, batch)
        command = build_command(
            node_executable,
            cli_script,
            batch,
            artifact_kind,
            batch_id,
        )
        print(
            "BATCH_START index={}/{} files={} requestId={}".format(
                index,
                len(batches),
                len(batch),
                batch_id,
            ),
            flush=True,
        )
        completed = run_command(command, shell=False, check=False)
        print(
            "BATCH_END index={}/{} exitCode={}".format(
                index,
                len(batches),
                completed.returncode,
            ),
            flush=True,
        )
        if completed.returncode != 0 and first_failure == 0:
            first_failure = completed.returncode
        if completed.returncode in FATAL_EXIT_CODES:
            break
    print("FINAL_EXIT_CODE={}".format(first_failure), flush=True)
    return first_failure
