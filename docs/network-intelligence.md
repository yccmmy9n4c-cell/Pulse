# Network Intelligence

Pulse Linux Beta 0.0.0.20 adds a dedicated Network Intelligence page for supported Debian-family desktops. The page and Dashboard domain score use the same six local evidence sources.

## Evidence cards

| Card | Source | Interpretation |
| --- | --- | --- |
| Active Interfaces | `ip -json link show up` | Reports active non-loopback interface names; no traffic is generated |
| Default Route | `ip -json -4/-6 route show default` | Reports whether local IPv4 and/or IPv6 default-route structure exists; it does not claim outside reachability |
| Network Manager | `nmcli ... general status` when installed | Reports the existing desktop management state; a missing optional tool is informational, not a network failure |
| DNS Configuration | `/etc/resolv.conf` | Counts configured nameserver entries and recognizes a local resolver stub without retaining addresses or sending a query |
| Listening Services | `ss -H -lntu` when installed | Counts local TCP/UDP listeners and all-address bindings without retaining endpoints, ports, process names, or payloads |
| Firewall Posture | `systemctl is-active` for UFW/nftables | Reports service indicators without claiming complete rule coverage |

## Safety and privacy boundary

Pulse does not run `ping`, `curl`, `wget`, `dig`, `host`, `nslookup`, `getent`, a speed test, or any public-service probe during assessment. It does not transmit resolver or socket details and does not change connections, routes, DNS, ports, services, or firewall rules.

An absent optional NetworkManager or `ss` command is incomplete coverage, not evidence of failure. A directly observed lack of active interfaces or default route requests review because those conditions may prevent expected connectivity, while still acknowledging an intentionally offline or isolated computer.

## Guided review

The page's **Open Network Settings** action tries only fixed, installed graphical tools:

- NetworkManager connection editor;
- GNOME Control Center's Network page;
- KDE System Settings network management.

When firewall evidence is the recommended review item, Pulse instead opens the installed GUFW interface when available. If it is absent, Pulse shows the detailed evidence and cautious service-indicator limitation rather than pretending a generic network panel can review firewall policy.

The user initiates the action. Pulse neither elevates nor changes settings, and it falls back to the detailed in-app assessment when no supported utility is installed.
