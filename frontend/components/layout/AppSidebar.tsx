"use client";

import Link from "next/link";
import { Brain, Package, Sparkles } from "lucide-react";
import { memo } from "react";
import { usePathname } from "next/navigation";

function AppSidebar() {
  const pathname = usePathname();

  const isActive = (path: string) =>
    pathname === path || pathname.startsWith(path + "/");
  return (
    <aside
      className="
        hidden
        lg:block
        bg-[#0f2740] text-white
      "
    >
      <div className="sticky top-0 h-screen overflow-y-auto p-6">
        {/* Branding */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold leading-none">Sistema</h1>
          <p className="text-sm text-gray-400 mt-1">Gerenciamento</p>
        </div>

        {/* Navegação */}
        <nav className="space-y-2">

          {/* Pedidos */}
          <Link
            href="/orders"
            className={`
              flex items-center gap-3 px-4 py-3 rounded-lg font-medium
              transition-colors
              ${
                isActive("/orders")
                  ? "bg-white/10 text-white"
                  : "text-gray-300 hover:bg-white/10 hover:text-white"
              }
            `}
          >
            <Package className="h-5 w-5 shrink-0" />
            <span>Pedidos</span>
          </Link>

          {/* Inteligência Artificial */}
          <Link
            href="/ia"
            className={`
              flex items-center gap-3 px-4 py-3 rounded-lg font-medium
              transition-colors
              ${
                isActive("/ia")
                  ? "bg-white/10 text-white"
                  : "text-gray-300 hover:bg-white/10 hover:text-white"
              }
            `}
          >
            <Sparkles className="h-5 w-5 shrink-0" />
            <span>Inteligência Artificial</span>
          </Link>
        </nav>
      </div>
    </aside>
  );
}

export default memo(AppSidebar);
