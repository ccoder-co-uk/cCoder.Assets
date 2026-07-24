"""Upsert generated cCoder.CodeAnalysis pages through the Content Management API."""

from __future__ import annotations

import argparse
import json
import os
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "Data" / "ccoder.co.uk" / "Default"
BASE_URL = "https://ccoder.co.uk/"


class Api:
    def __init__(self) -> None:
        self.token: str | None = None

    def request(self, method: str, path: str, value: object | None = None) -> object:
        body = None if value is None else json.dumps(value).encode("utf-8")
        headers = {"Accept": "application/json"}
        if body is not None:
            headers["Content-Type"] = "application/json"
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        request = urllib.request.Request(
            urllib.parse.urljoin(BASE_URL, path),
            data=body,
            headers=headers,
            method=method,
        )
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                content = response.read()
        except urllib.error.HTTPError as error:
            detail = error.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"{method} {path} returned {error.code}: {detail}") from error
        return json.loads(content) if content else None

    def login(self, user: str, password: str) -> None:
        response = self.request("POST", "Api/Account/Login", {"User": user, "Pass": password})
        self.token = response.get("id") or response.get("Id")
        if not self.token:
            raise RuntimeError("The login response did not contain a bearer token.")


def collection(value: object) -> list[dict]:
    if isinstance(value, list):
        return value
    return value.get("value") or value.get("Value") or []


def canonical(path: str) -> str:
    return path.strip("/")


def source_pages() -> list[tuple[Path, dict]]:
    files = list((DATA / "Pages").glob("Documentation_CodeAnalysis*.json"))
    files.append(DATA / "Pages" / "_Platform-Domains_Code-Analysis.json")
    pages = [(path, json.loads(path.read_text(encoding="utf-8"))) for path in files]
    return sorted(pages, key=lambda item: (canonical(item[1]["Path"]).count("/"), item[1]["Order"]))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--remove-failed-draft", action="store_true")
    arguments = parser.parse_args()

    user = os.environ.get("CCODER_USER")
    password = os.environ.get("CCODER_PASSWORD")
    if not user or not password:
        raise RuntimeError("CCODER_USER and CCODER_PASSWORD are required.")

    api = Api()
    api.login(user, password)

    apps = collection(
        api.request(
            "GET",
            "Api/ContentManagement/App?$filter="
            + urllib.parse.quote("Domain eq 'ccoder.co.uk'")
            + "&$top=2",
        )
    )
    if len(apps) != 1:
        raise RuntimeError(f"Expected one ccoder.co.uk app, found {len(apps)}.")
    app_id = apps[0].get("Id") or apps[0].get("id")

    existing_pages = collection(
        api.request(
            "GET",
            f"Api/ContentManagement/Page?$filter={urllib.parse.quote(f'AppId eq {app_id}')}&$top=1000",
        )
    )
    pages_by_path = {
        canonical(page.get("Path") or page.get("path") or ""): page for page in existing_pages
    }

    if arguments.remove_failed_draft:
        failed_roots = ("Documentation/cCoder.CodeAnalysis", "Platform-Domains/cCoder.CodeAnalysis")
        failed_pages = [
            page
            for path, page in pages_by_path.items()
            if any(path == root or path.startswith(root + "/") for root in failed_roots)
        ]
        for failed_page in sorted(
            failed_pages,
            key=lambda page: canonical(page.get("Path") or page.get("path") or "").count("/"),
            reverse=True,
        ):
            page_id = failed_page.get("Id") or failed_page.get("id")
            api.request("DELETE", f"Api/ContentManagement/Page({page_id})")
        if failed_pages:
            print(f"Removed {len(failed_pages)} pages from the failed path draft.")
            existing_pages = collection(
                api.request(
                    "GET",
                    f"Api/ContentManagement/Page?$filter={urllib.parse.quote(f'AppId eq {app_id}')}&$top=1000",
                )
            )
            pages_by_path = {
                canonical(page.get("Path") or page.get("path") or ""): page
                for page in existing_pages
            }

    planned = source_pages()
    print(
        f"Authenticated; {len(planned)} CodeAnalysis pages are ready "
        f"({sum(canonical(page['Path']) in pages_by_path for _, page in planned)} updates, "
        f"{sum(canonical(page['Path']) not in pages_by_path for _, page in planned)} creates)."
    )
    if not arguments.apply:
        return

    published: dict[str, dict] = {}
    created_paths: set[str] = set()
    for _, source in planned:
        path = canonical(source["Path"])
        parent_path = path.rsplit("/", 1)[0] if "/" in path else ""
        parent = published.get(parent_path) or pages_by_path.get(parent_path)
        if parent is None:
            raise RuntimeError(f"Parent page '{parent_path}' was not found for '{path}'.")
        existing = pages_by_path.get(path)
        payload = {
            "Id": (existing or {}).get("Id") or (existing or {}).get("id") or 0,
            "ParentId": parent.get("Id") or parent.get("id"),
            "AppId": app_id,
            "Order": source["Order"],
            "ShowOnMenus": source["ShowOnMenus"],
            "Name": source["Name"],
            "ResourceKey": source.get("ResourceKey", "Default"),
            "Layout": source["Layout"],
            "PageInfo": source["PageInfo"],
            "Contents": source["Contents"],
        }
        if existing:
            page = api.request(
                "PUT",
                f"Api/ContentManagement/Page({payload['Id']})",
                payload,
            )
        else:
            page = api.request("POST", "Api/ContentManagement/Page", payload)
            created_paths.add(path)
        published[path] = page
        pages_by_path[path] = page

    role_files = list((DATA / "PageRoles").glob("Documentation_CodeAnalysis*.json"))
    role_files += list((DATA / "PageRoles").glob("Platform-Domains_Code-Analysis*.json"))
    security_roles = collection(api.request("GET", "Api/AppSecurity/Role?$top=100"))
    role_ids = {
        (role.get("Name") or role.get("name")): (role.get("Id") or role.get("id"))
        for role in security_roles
    }
    created_roles = 0
    for role_file in role_files:
        role = json.loads(role_file.read_text(encoding="utf-8"))
        if canonical(role["Path"]) not in created_paths:
            continue
        page = pages_by_path[canonical(role["Path"])]
        page_id = page.get("Id") or page.get("id")
        role_id = role_ids.get(role["Role"])
        if not role_id:
            raise RuntimeError(f"Security role '{role['Role']}' was not found.")
        existing_roles = collection(
            api.request(
                "GET",
                "Api/ContentManagement/PageRole?$filter="
                + urllib.parse.quote(f"PageId eq {page_id} and RoleId eq {role_id}")
                + "&$top=1",
            )
        )
        if not existing_roles:
            api.request(
                "POST",
                "Api/ContentManagement/PageRole",
                {"PageId": page_id, "RoleId": role_id},
            )
            created_roles += 1

    print(f"Published {len(published)} pages and created {created_roles} missing page-role links.")


if __name__ == "__main__":
    main()
