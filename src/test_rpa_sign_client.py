import subprocess
import unittest
from pathlib import Path

from rpa_sign_client import build_command, normalize_files, run_sign_batches, split_file_batches


class RpaSignClientTests(unittest.TestCase):
    def test_normalizes_and_deduplicates_targets(self):
        files = normalize_files([
            r"C:\out\b.dll",
            r"C:\out\a.exe",
            r"c:\OUT\B.DLL",
        ])
        self.assertEqual(files, [r"C:\out\a.exe", r"C:\out\b.dll"])

    def test_splits_by_file_count_and_command_length(self):
        files = [r"C:\out\f{:04d}.dll".format(index) for index in range(257)]
        batches = split_file_batches(
            r"C:\node\node.exe",
            r"C:\controller\bin\sign-cli.js",
            files,
            "ii-rpa-binaries",
            "pipeline-1",
        )
        self.assertEqual([len(batch) for batch in batches], [256, 1])

        long_files = [
            "C:\\out\\{}\\f{:04d}.dll".format("nested" * 30, index)
            for index in range(100)
        ]
        long_batches = split_file_batches(
            r"C:\node\node.exe",
            r"C:\controller\bin\sign-cli.js",
            long_files,
            "ii-rpa-binaries",
            "pipeline-1",
            max_chars=1800,
        )
        self.assertGreater(len(long_batches), 1)
        for index, batch in enumerate(long_batches, start=1):
            command = build_command(
                r"C:\node\node.exe",
                r"C:\controller\bin\sign-cli.js",
                batch,
                "ii-rpa-binaries",
                "pipeline-1:chunk-{:04d}".format(index),
            )
            self.assertLessEqual(len(subprocess.list2cmdline(command)), 1800)

    def test_propagates_first_failure_and_uses_shell_false(self):
        calls = []
        return_codes = iter([13, 0])

        def fake_run(command, **kwargs):
            calls.append((command, kwargs))
            return subprocess.CompletedProcess(command, next(return_codes))

        code = run_sign_batches(
            r"C:\node\node.exe",
            r"C:\controller\bin\sign-cli.js",
            [r"C:\out\a.dll", r"C:\out\b.dll"],
            "ii-rpa-binaries",
            "pipeline-1",
            max_files=1,
            run_command=fake_run,
        )

        self.assertEqual(code, 13)
        self.assertEqual(len(calls), 2)
        self.assertTrue(all(item[1]["shell"] is False for item in calls))

    def test_legacy_signature_entrypoints_use_centralized_signing_only(self):
        repo_root = Path(__file__).resolve().parents[1]
        source_entrypoint = repo_root / "src" / "Signature.bat"
        event_monitor_entrypoint = (
            repo_root / "tools" / "II.RPA.EventMonitor" / "Signature.bat"
        )

        self.assertTrue(source_entrypoint.is_file())
        self.assertTrue(event_monitor_entrypoint.is_file())

        source_content = source_entrypoint.read_text(encoding="utf-8").lower()
        event_monitor_content = event_monitor_entrypoint.read_text(
            encoding="utf-8"
        ).lower()

        self.assertIn("python sign.py", source_content)
        self.assertIn("%rpa_sign_cli%", event_monitor_content)
        self.assertIn("%rpa_sign_cli_script%", event_monitor_content)
        self.assertIn("\\*.dll", event_monitor_content)
        self.assertIn("\\*.exe", event_monitor_content)
        self.assertNotIn("signtool", source_content + event_monitor_content)
        self.assertNotIn(".pfx", source_content + event_monitor_content)


if __name__ == "__main__":
    unittest.main()
