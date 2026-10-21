"""Run a mandatory focused Python suite; empty or skipped suites fail."""

import argparse
import json
from pathlib import Path
import sys
import unittest


ROOT = Path(__file__).resolve().parents[1]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", default="scripts/tests")
    parser.add_argument("--pattern", required=True)
    arguments = parser.parse_args(argv)
    sys.path.insert(0, str(ROOT))
    suite = unittest.TestLoader().discover(arguments.directory, pattern=arguments.pattern)
    if suite.countTestCases() == 0:
        print(json.dumps({"eventCode": "required_python_tests_missing"}), file=sys.stderr)
        return 1
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    return 0 if result.wasSuccessful() and not result.skipped else 1


if __name__ == "__main__":
    raise SystemExit(main())
